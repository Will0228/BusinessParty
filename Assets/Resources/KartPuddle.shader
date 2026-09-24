Shader "MixVerse/KartPuddle"
{
    Properties
    {
        [Header(Shape)]
        _WaterLevel ("Water Level", Range(0.2, 0.8)) = 0.6
        _EdgeSoftness ("Edge Softness", Range(0.005, 0.2)) = 0.03
        _WetRimWidth ("Wet Rim Width", Range(0, 0.5)) = 0.2
        _ShapeNoise ("Shape Noise", Range(0, 1)) = 0.6

        [Header(Surface)]
        _WaterTransmission ("Water Transmission", Range(0, 1)) = 0.45
        _RimTransmission ("Wet Rim Transmission", Range(0, 1)) = 0.75
        _Reflectivity ("Reflectivity", Range(0, 4)) = 2
        _RimReflectivity ("Wet Rim Reflectivity", Range(0, 1)) = 0.3
        _RimRoughness ("Wet Rim Roughness", Range(0, 1)) = 0.35
        _SpecularPower ("Specular Power", Range(16, 2048)) = 700
        _SpecularIntensity ("Specular Intensity", Range(0, 10)) = 4

        [Header(Ripples)]
        _RainAmount ("Rain Amount", Range(0, 1)) = 1
        _RippleScale ("Ripple Cells per Meter", Range(0.2, 6)) = 1.4
        _RippleSpeed ("Ripple Speed", Range(0, 4)) = 1.1
        _RippleStrength ("Ripple Strength", Range(0, 2)) = 0.6
        _RippleDensity ("Ripple Density", Range(0, 1)) = 0.65
        _WindWaveStrength ("Wind Wave Strength", Range(0, 0.3)) = 0.05

        [Header(Sky)]
        [NoScaleOffset] _SkyCube ("Sky Cubemap", Cube) = "black" {}
        _SkyIntensity ("Sky Intensity", Range(0, 4)) = 1

        [Header(Screen Space Reflection)]
        _SSRMaxDistance ("Max Distance", Range(5, 200)) = 90
        _SSRThickness ("Thickness", Range(0.05, 5)) = 1.5
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent-20"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }
        Pass
        {
            Name "Puddle"
            Tags { "LightMode" = "UniversalForward" }
            // 下の路面を透過率のぶんだけ暗くし、その上に反射を加算する
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            Offset -1, -1

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            #define SSR_STEPS 24
            #define SSR_REFINE_STEPS 4

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                // xy: 水溜りメッシュ内の -1..1 座標, z: 形を変えるための seed
                float4 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 uv : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float _WaterLevel;
                float _EdgeSoftness;
                float _WetRimWidth;
                float _ShapeNoise;
                float _WaterTransmission;
                float _RimTransmission;
                float _Reflectivity;
                float _RimReflectivity;
                float _RimRoughness;
                float _SpecularPower;
                float _SpecularIntensity;
                float _RainAmount;
                float _RippleScale;
                float _RippleSpeed;
                float _RippleStrength;
                float _RippleDensity;
                float _WindWaveStrength;
                float _SkyIntensity;
                float _SSRMaxDistance;
                float _SSRThickness;
            CBUFFER_END

            TEXTURECUBE(_SkyCube);
            SAMPLER(sampler_SkyCube);

            float Hash21(float2 p)
            {
                float3 p3 = frac(p.xyx * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float2 Hash22(float2 p)
            {
                float3 p3 = frac(p.xyx * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    value += ValueNoise(p) * amplitude;
                    p = p * 2.03 + 17.1;
                    amplitude *= 0.5;
                }
                return value;
            }

            // 中心 0・メッシュの縁で 1 を超える距離場。ドメインワープで輪郭を崩して不定形にする
            float PuddleField(float2 p, float seed)
            {
                float2 offset = float2(37.1, 91.7) * seed;
                float2 warp = float2(Fbm(p * 1.3 + offset), Fbm(p * 1.3 + offset + 5.2)) - 0.47;
                float2 q = p + warp * 0.5;
                float lobes = Fbm(q * 2.2 + offset + 11.3) - 0.47;
                float border = smoothstep(0.6, 1.0, max(abs(p.x), abs(p.y)));
                return length(q) + lobes * _ShapeNoise + border;
            }

            // 雨粒の落ちた点から広がる波紋の傾き。セルごとに周期をずらし、周期ごとに落下位置・大きさ・有無を振り直す
            float2 RainRippleSlope(float2 positionXZ)
            {
                float2 slope = 0.0;
                [unroll]
                for (int layer = 0; layer < 3; layer++)
                {
                    float2 grid = positionXZ * _RippleScale + float2(0.37, 0.71) * layer;
                    float2 cell = floor(grid);
                    float phase = _Time.y * _RippleSpeed * (0.8 + 0.15 * layer) + Hash21(cell + 19.3 * layer) * 7.13;
                    float life = frac(phase);
                    float2 drop = Hash22(cell + floor(phase) * 13.7 + 41.7 * layer);
                    float2 shape = Hash22(cell - floor(phase) * 5.3 + 73.1 * layer);
                    float2 delta = frac(grid) - (0.25 + 0.5 * drop);
                    float radius = length(delta);
                    float front = radius - life * lerp(0.14, 0.3, shape.x);
                    float band = saturate(1.0 - abs(front) / 0.05);
                    float fade = (1.0 - life) * (1.0 - life) * (1.0 - life) * step(shape.y, _RippleDensity);
                    slope += delta / max(radius, 0.0001) * cos(front * 90.0) * band * band * fade;
                }
                return slope;
            }

            float2 WindSlope(float2 positionXZ)
            {
                float2 p = positionXZ * 0.8 + _Time.y * float2(0.3, 0.17);
                return float2(ValueNoise(p), ValueNoise(p + 13.7)) - 0.5;
            }

            float2 ClipToScreenUv(float4 positionCS)
            {
                float2 ndc = positionCS.xy / positionCS.w;
                ndc.y *= _ProjectionParams.x;
                return ndc * 0.5 + 0.5;
            }

            // レイマーチのループ内で使うため、勾配を使わない LOD 0 サンプルにしている
            float SceneEyeDepth(float2 uv)
            {
                float2 scaled = ClampAndScaleUVForBilinear(UnityStereoTransformScreenSpaceTex(uv), _CameraDepthTexture_TexelSize.xy);
                return LinearEyeDepth(SAMPLE_TEXTURE2D_X_LOD(_CameraDepthTexture, sampler_PointClamp, scaled, 0).r, _ZBufferParams);
            }

            half3 SceneColor(float2 uv)
            {
                float2 scaled = ClampAndScaleUVForBilinear(UnityStereoTransformScreenSpaceTex(uv), _CameraOpaqueTexture_TexelSize.xy);
                return SAMPLE_TEXTURE2D_X_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, scaled, 0).rgb;
            }

            // 反射ベクトルに沿って深度バッファを辿り、最初に遮られた画面位置を返す。当たらなければ fade は 0
            float2 TraceReflection(float3 origin, float3 direction, float jitter, out float fade)
            {
                fade = 0.0;
                float previous = 0.0;
                float travel = 0.25 + jitter * 0.35;
                [loop]
                for (int i = 0; i < SSR_STEPS; i++)
                {
                    float4 positionCS = TransformWorldToHClip(origin + direction * travel);
                    float2 uv = ClipToScreenUv(positionCS);
                    if (positionCS.w <= 0.0 || any(uv < 0.0) || any(uv > 1.0)) break;
                    float behind = positionCS.w - SceneEyeDepth(uv);
                    if (behind > 0.0 && behind < _SSRThickness + travel - previous)
                    {
                        float low = previous;
                        float high = travel;
                        [unroll]
                        for (int j = 0; j < SSR_REFINE_STEPS; j++)
                        {
                            float middle = (low + high) * 0.5;
                            float4 refineCS = TransformWorldToHClip(origin + direction * middle);
                            if (refineCS.w - SceneEyeDepth(ClipToScreenUv(refineCS)) > 0.0) high = middle;
                            else low = middle;
                        }
                        float2 hitUv = ClipToScreenUv(TransformWorldToHClip(origin + direction * high));
                        float2 edge = smoothstep(0.0, 0.06, hitUv) * smoothstep(1.0, 0.94, hitUv);
                        fade = edge.x * edge.y * (1.0 - smoothstep(_SSRMaxDistance * 0.6, _SSRMaxDistance, high));
                        return hitUv;
                    }
                    previous = travel;
                    travel *= 1.28;
                    if (travel > _SSRMaxDistance) break;
                }
                return float2(0.0, 0.0);
            }

            // レイが何にも当たらなかった方向は、カメラから同じ方向に見えている遠景で代用し、画面外なら夜空を使う
            half3 FarReflection(float3 direction, float roughness)
            {
                half3 sky = SAMPLE_TEXTURECUBE_LOD(_SkyCube, sampler_SkyCube, direction, roughness * 6.0).rgb * _SkyIntensity;
                float4 farCS = mul(GetWorldToHClipMatrix(), float4(direction, 0.0));
                if (farCS.w <= 0.0) return sky;
                float2 uv = ClipToScreenUv(farCS);
                float2 edge = smoothstep(0.0, 0.08, uv) * smoothstep(1.0, 0.92, uv);
                return lerp(sky, SceneColor(saturate(uv)), edge.x * edge.y);
            }

            half3 SpecularHighlight(Light light, float3 normalWS, float3 viewWS)
            {
                float3 halfVector = SafeNormalize(light.direction + viewWS);
                float highlight = pow(saturate(dot(normalWS, halfVector)), _SpecularPower);
                return light.color * (light.distanceAttenuation * light.shadowAttenuation * highlight * _SpecularIntensity);
            }

            half3 LightHighlights(float3 positionWS, float3 normalWS, float3 viewWS, float2 screenUv)
            {
                half3 specular = SpecularHighlight(GetMainLight(), normalWS, viewWS);
                #if defined(_ADDITIONAL_LIGHTS)
                InputData inputData = (InputData)0;
                inputData.positionWS = positionWS;
                inputData.normalizedScreenSpaceUV = screenUv;
                uint pixelLightCount = GetAdditionalLightsCount();
                #if USE_CLUSTER_LIGHT_LOOP
                [loop]
                for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                    specular += SpecularHighlight(GetAdditionalLight(lightIndex, positionWS), normalWS, viewWS);
                }
                #endif
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    specular += SpecularHighlight(GetAdditionalLight(lightIndex, positionWS), normalWS, viewWS);
                LIGHT_LOOP_END
                #endif
                return specular;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(position.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 positionWS = input.positionWS;
                float field = PuddleField(input.uv.xy, input.uv.z);
                float aa = max(fwidth(field), 0.0001);
                float water = 1.0 - smoothstep(_WaterLevel - _EdgeSoftness - aa, _WaterLevel + aa, field);
                float wet = 1.0 - smoothstep(_WaterLevel, _WaterLevel + _WetRimWidth + aa, field);
                // 遠くでは波紋の縞が画素より細かくなってちらつくので弱める
                float2 rippleGrid = positionWS.xz * _RippleScale;
                float rippleLod = 1.0 - smoothstep(0.04, 0.12, length(fwidth(rippleGrid)));
                if (wet <= 0.001) return half4(0.0, 0.0, 0.0, 0.0);

                float3 surfaceNormal = normalize(input.normalWS);
                float2 slope = RainRippleSlope(positionWS.xz) * (_RippleStrength * _RainAmount * rippleLod);
                slope += WindSlope(positionWS.xz) * _WindWaveStrength;
                float2 roughness = (float2(ValueNoise(positionWS.xz * 3.0), ValueNoise(positionWS.xz * 3.0 + 3.1)) - 0.5) * _RimRoughness;
                slope = lerp(roughness, slope, water);
                float3 normalWS = normalize(surfaceNormal + float3(slope.x, 0.0, slope.y));

                float3 viewWS = GetWorldSpaceNormalizeViewDir(positionWS);
                float3 reflected = reflect(-viewWS, normalWS);
                float rise = dot(reflected, surfaceNormal);
                if (rise < 0.02) reflected = normalize(reflected + surfaceNormal * (0.02 - rise));

                float fresnel = 0.02 + 0.98 * pow(1.0 - saturate(dot(normalWS, viewWS)), 5.0);
                float reflectivity = saturate(fresnel * _Reflectivity) * lerp(_RimReflectivity, 1.0, water) * wet;

                float2 screenUv = GetNormalizedScreenSpaceUV(input.positionCS);
                half3 reflection = FarReflection(reflected, lerp(_RimRoughness, 0.02, water));
                if (reflectivity > 0.005)
                {
                    float jitter = frac(52.9829189 * frac(dot(input.positionCS.xy, float2(0.06711056, 0.00583715))));
                    float hitFade;
                    float2 hitUv = TraceReflection(positionWS + surfaceNormal * 0.03, reflected, jitter, hitFade);
                    reflection = lerp(reflection, SceneColor(hitUv), hitFade);
                }

                half3 color = reflection * reflectivity + LightHighlights(positionWS, normalWS, viewWS, screenUv) * water;
                float transmission = lerp(_RimTransmission, _WaterTransmission, water);
                half alpha = wet * (1.0 - transmission);

                // 下地の路面はフォグ済みなので、自分が塗り替えるぶんだけフォグ色を足して遠景へ溶かす
                float fogVisibility = MixFogColor(half3(1.0, 1.0, 1.0), half3(0.0, 0.0, 0.0), InitializeInputDataFog(float4(positionWS, 1.0), input.fogFactor)).r;
                color = color * fogVisibility + unity_FogColor.rgb * ((1.0 - fogVisibility) * alpha);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
