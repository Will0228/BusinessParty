Shader "MixVerse/RainWindshieldShaderURP"
{
    Properties
    {
        [Header(Rain)]
        _RainAmount ("Rain Amount", Range(0, 1)) = 1

        [Header(Droplets)]
        _DropletMinRadius ("Droplet Min Radius", Range(0.02, 0.3)) = 0.03
        _DropletMaxRadius ("Droplet Max Radius", Range(0.02, 0.3)) = 0.14
        _SizeVariation ("Size Variation", Range(0, 1)) = 1

        [Header(Streaks)]
        _StreakDensityX ("Streak Density X", Range(4, 40)) = 20
        _StreakDensityY ("Streak Density Y", Range(1, 10)) = 5
        _StreakCoverage ("Streak Coverage", Range(0, 1)) = 0.65
        _FallSpeed ("Fall Speed", Range(0, 6)) = 0.65
        _SpeedVariation ("Speed Variation", Range(0, 1)) = 0.85
        _DirectionVariation ("Direction Variation", Range(0, 1)) = 0.85
        _WindSkew ("Wind Skew", Range(-1, 1)) = 0.08
        _TrailLength ("Trail Length", Range(0, 0.8)) = 0.5

        [Header(Look)]
        _RefractionStrength ("Refraction Strength", Range(0, 0.2)) = 0.035
        _NormalStrength ("Surface Curvature", Range(0.1, 3)) = 1.2
        _HighlightIntensity ("Highlight Intensity", Range(0, 4)) = 0.45
        _HighlightPower ("Highlight Power", Range(1, 128)) = 72
        _RimColor ("Reflection Color", Color) = (0.85, 0.92, 1, 1)
        _TintColor ("Water Transmission", Color) = (0.88, 0.96, 1, 1)
        _TintAmount ("Tint Amount", Range(0, 1)) = 0.08
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float _RainAmount;
                float _DropletMinRadius;
                float _DropletMaxRadius;
                float _SizeVariation;
                float _StreakDensityX;
                float _StreakDensityY;
                float _StreakCoverage;
                float _FallSpeed;
                float _SpeedVariation;
                float _DirectionVariation;
                float _WindSkew;
                float _TrailLength;
                float _RefractionStrength;
                float _NormalStrength;
                float _HighlightIntensity;
                float _HighlightPower;
                half4 _RimColor;
                half4 _TintColor;
                float _TintAmount;
            CBUFFER_END

            float3 Hash23(float2 cell)
            {
                float3 p = frac(float3(cell.x, cell.y, cell.x) * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yzx + 33.33);
                return frac((p.xxy + p.yzz) * p.zyx);
            }

            struct WaterSurface
            {
                float height;
                float2 slope;
                float mask;
            };

            WaterSurface Bead(float2 delta, float2 radius, float thickness, float pixelSize)
            {
                WaterSurface surface = (WaterSurface)0;
                float2 p = delta / max(radius, 0.00001);
                float r2 = dot(p, p);
                float edge = max(pixelSize / min(radius.x, radius.y), 0.015);
                surface.mask = 1.0 - smoothstep(1.0 - edge, 1.0 + edge, sqrt(r2));
                float cap = sqrt(max(1.0 - 0.96 * r2, 0.04));
                surface.height = thickness * max(cap - 0.2, 0.0);
                surface.slope = -0.96 * thickness * p / (radius * cap) * surface.mask;
                return surface;
            }

            WaterSurface Merge(WaterSurface a, WaterSurface b)
            {
                if (a.mask <= 0.0) return b;
                if (b.mask <= 0.0) return a;

                // 重なった滴の高さと傾きを滑らかにつなぎ、接合部の法線の切り替わりを抑える。
                float weight = saturate(0.5 + (b.height - a.height) / 0.0015);
                WaterSurface surface;
                surface.height = lerp(a.height, b.height, weight);
                surface.slope = lerp(a.slope, b.slope, weight);
                surface.mask = max(a.mask, b.mask);
                return surface;
            }

            WaterSurface FlowingDrops(float2 uv, float pixelSize)
            {
                WaterSurface surface = (WaterSurface)0;
                float2 density = float2(_StreakDensityX, _StreakDensityY);
                float2 shearedUv = float2(uv.x + uv.y * _WindSkew, uv.y);
                float baseColumn = floor(shearedUv.x * density.x);

                // 蛇行と加減速でセルをまたぐ滴も、同じIDで隣接セルから評価する。
                [unroll]
                for (int columnOffset = -1; columnOffset <= 1; columnOffset++)
                {
                    float column = baseColumn + columnOffset;
                    float3 columnRandom = Hash23(float2(column, 57.0));
                    float speed = lerp(1.0, lerp(0.3, 1.7, columnRandom.x), _SpeedVariation);
                    float time = _Time.y * _FallSpeed * speed;
                    float2 grid = shearedUv * density + float2(0.0, time);
                    float2 cell = float2(column, floor(grid.y));

                    [unroll]
                    for (int row = -1; row <= 1; row++)
                    {
                        float2 id = cell - float2(0.0, row);
                        float3 random = Hash23(id + 173.2);
                        float3 motion = Hash23(id + 419.7);
                        float exists = step(random.z, _StreakCoverage);
                        float2 local = (grid - id - 0.5) / density;
                        float centerX = (random.x - 0.5) * 0.45 / density.x;
                        float bendFrequency = lerp(12.0, 32.0, motion.x);
                        float bendPhase = uv.y * bendFrequency + motion.y * 6.283185;
                        float bendAmplitude = 0.14 * _DirectionVariation / density.x;
                        float bend = sin(bendPhase) * bendAmplitude;
                        float bendSlope = cos(bendPhase) * bendFrequency * bendAmplitude;
                        float radiusSample = lerp(0.5, random.y * random.y, _SizeVariation);
                        float radius = min(lerp(_DropletMinRadius, _DropletMaxRadius, radiusSample) / density.x, 0.28 / density.y);
                        float pulse = sin(time * lerp(0.7, 2.4, motion.z) + motion.x * 6.283185);
                        float centerY = ((random.z - 0.5) * 0.2 + pulse * 0.22 * _SpeedVariation) / density.y;
                        float2 delta = float2(local.x - centerX - bend, local.y - centerY);
                        WaterSurface head = Bead(delta, radius * float2(1.0, 1.25), radius * 0.8, pixelSize);

                        float trailLength = _TrailLength / density.y;
                        float along = saturate(delta.y / max(trailLength, 0.0001));
                        float taper = 1.0 - along;
                        float width = radius * lerp(0.08, 0.24, taper);
                        float across = delta.x / max(width, 0.00001);
                        float profile = saturate(1.0 - across * across);
                        float envelope = smoothstep(0.0, radius, delta.y) * taper * taper;
                        float breakup = lerp(0.35, 1.0, 0.5 + 0.5 * sin(uv.y * 280.0 + random.x * 30.0));
                        WaterSurface trail = (WaterSurface)0;
                        trail.height = width * 0.3 * profile * envelope * breakup;
                        trail.slope = float2(-0.6 * across * envelope * breakup * step(abs(across), 1.0), 0.0);
                        trail.mask = (1.0 - smoothstep(max(width - pixelSize, 0.0), width + pixelSize, abs(delta.x))) * envelope * breakup;
                        WaterSurface drop = Merge(head, trail);
                        drop.slope.y += drop.slope.x * (_WindSkew - bendSlope);
                        drop.height *= exists;
                        drop.slope *= exists;
                        drop.mask *= exists;
                        surface = Merge(surface, drop);
                    }
                }
                return surface;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);
                float pixelSize = 1.0 / max(_ScreenParams.y, 1.0);
                float2 uv = float2(input.uv.x * aspect, input.uv.y);
                WaterSurface water = FlowingDrops(uv, pixelSize);
                float alpha = water.mask * saturate(_RainAmount) * input.color.a;
                if (alpha <= 0.001) return half4(0.0, 0.0, 0.0, 0.0);

                float3 normal = normalize(float3(-water.slope * _NormalStrength, 1.0));
                float3 transmitted = refract(float3(0.0, 0.0, -1.0), normal, 1.0 / 1.333);
                float lensDepth = _RefractionStrength * saturate(water.height * 160.0);
                float2 offset = transmitted.xy / max(-transmitted.z, 0.1) * lensDepth;
                offset.x /= aspect;
                float2 screenUv = GetNormalizedScreenSpaceUV(input.positionCS);
                float2 margin = _CameraOpaqueTexture_TexelSize.xy * 0.5;
                half3 scene = SampleSceneColor(clamp(screenUv + offset, margin, 1.0 - margin));

                float fresnel = 0.0204 + 0.9796 * pow(1.0 - normal.z, 5.0);
                float2 reflectionOffset = normal.xy * float2(0.06 / aspect, 0.06);
                half3 reflection = SampleSceneColor(clamp(screenUv + reflectionOffset, margin, 1.0 - margin));
                float3 halfVector = normalize(float3(-0.45, 0.7, 1.5));
                float highlight = pow(saturate(dot(normal, halfVector)), _HighlightPower) * _HighlightIntensity;
                half3 transmission = scene * lerp(half3(1, 1, 1), _TintColor.rgb, _TintAmount);
                half3 color = lerp(transmission, reflection * _RimColor.rgb, fresnel);
                color += highlight * _RimColor.rgb;
                return half4(color * input.color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
