Shader "MixVerse/WaterWaveShaderURP"
{
    Properties
    {
        [Header(Color)]
        _BaseColor ("Base Color", Color) = (0.09, 0.33, 0.5, 1)
        _CrestColor ("Crest Color", Color) = (0.3, 0.55, 0.65, 1)
        _CrestReference ("Crest Reference Height", Range(0.001, 2)) = 0.35

        [Header(Lighting)]
        _Smoothness ("Smoothness", Range(0, 1)) = 0.75
        _SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        _FresnelColor ("Fresnel Color", Color) = (0.8, 0.92, 1, 1)
        _FresnelPower ("Fresnel Power", Range(0.1, 8)) = 3

        [Header(Floater Foam)]
        _FoamColor ("Foam Color", Color) = (0.95, 0.97, 1, 1)
        _FoamOpacity ("Foam Opacity", Range(0, 1)) = 0.45
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        LOD 100

        Pass
        {
            Name "WaterSurface"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // C# 側の WaterWaveSimulator が確保する波紋バッファの最大数。
            // 変えるときは WaterWaveSimulator.RippleMax と揃えること。
            #define RIPPLE_MAX 16

            // C# 側の WaterWaveSurface が確保する浮遊物バッファの最大数。
            // 変えるときは WaterWaveSurface.FloaterMax と揃えること。
            #define FLOATER_MAX 8

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float waveHeight : TEXCOORD3;
                float2 localXZ : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _CrestColor;
                float _CrestReference;
                float _Smoothness;
                half4 _SpecularColor;
                half4 _FresnelColor;
                float _FresnelPower;
                half4 _FoamColor;
                float _FoamOpacity;

                // xy = 波紋の中心(オブジェクト空間の X, Z), z = 発生時刻, w = 寿命(秒)
                float4 _RippleData0[RIPPLE_MAX];
                // x = 振幅, y = 波長, z = 伝わる速さ, w = 1周期ごとに振幅が何倍に減るか(0~1)
                float4 _RippleData1[RIPPLE_MAX];
                // xy = 浮遊物の位置(オブジェクト空間の X, Z), z = 泡の半径, w = 強さ(0で無効)
                float4 _FloaterData[FLOATER_MAX];
            CBUFFER_END

            /// <summary>
            /// 波紋 1 個ぶんの、ある地点・ある時刻での寄与を足し込む。
            /// 水滴が落ちた地点から輪が広がるだけの円形波なので、位相は「中心からの距離」と
            /// 「経過時間」だけで決まる Sin 波として扱う。複数の波が重なったときの盛り上がりや
            /// 打ち消し合いは、height をここでただ足し合わせるだけで自然に表現できる
            /// (波の重ね合わせの原理。振幅がマイナスになる位置ではきちんと打ち消し合う)。
            /// </summary>
            void AccumulateRipple(int index, float2 posXZ, inout float height, inout float2 slope)
            {
                float2 origin = _RippleData0[index].xy;
                float startTime = _RippleData0[index].z;
                float life = _RippleData0[index].w;
                float amplitude = _RippleData1[index].x;
                float wavelength = _RippleData1[index].y;
                float speed = _RippleData1[index].z;
                float decayPerPeriod = _RippleData1[index].w;

                float age = _Time.y - startTime;

                if (amplitude <= 0.0 || age < 0.0 || age >= life)
                {
                    return;
                }

                float2 delta = posXZ - origin;
                float r = length(delta);
                float2 dir = r > 1e-4 ? (delta / r) : float2(0.0, 0.0);

                float k = TWO_PI / max(wavelength, 1e-4);
                float omega = k * speed;

                // 波面(速さ speed で広がるリング)より内側だけを振動させる。
                // 立ち上がり・後方の減衰は 1 波長ぶんかけてなめらかにする。
                float front = speed * age;
                float frontFade = 1.0 - smoothstep(front - wavelength, front + wavelength, r);

                // 円形に広がる波はエネルギーが円周に分散するぶん、遠いほど振幅が下がる。
                float spread = rsqrt(max(r, wavelength * 0.25));

                // その場で 1 往復(1周期)するたびに振幅が decayPerPeriod 倍になる減衰。
                // 実際の水面と同じく、同じ場所で何度も同じ高さで揺れ続けたりしないよう、
                // 山が来るたびに前の山よりはっきり低くなる。
                float period = wavelength / max(speed, 1e-4);
                float timeDecay = pow(max(decayPerPeriod, 1e-4), age / max(period, 1e-4));

                float envelope = amplitude * frontFade * spread * timeDecay;

                float s, c;
                sincos(k * r - omega * age, s, c);

                height += envelope * s;

                // 振幅側(spread や frontFade)の空間変化は無視し、位相の勾配だけで法線を近似する。
                // 見た目のうねりには十分な精度で、計算も軽い。
                slope += envelope * k * c * dir;
            }

            /// <summary>
            /// 浮遊物 1 個ぶんの、水面が泡立って見える寄与を足し込む。
            /// 途中で平らになる部分を作らず、中心から縁まで一様になだらかに(2乗の減衰で)
            /// 弱まっていくことで、水の色との境目がくっきり出ないようにしている。
            /// 実際の色への反映は _FoamOpacity で頭打ちにするので、ここでは 0~1 の強さだけを返す。
            /// </summary>
            void AccumulateFoam(int index, float2 posXZ, inout float foam)
            {
                float strength = _FloaterData[index].w;

                if (strength <= 0.0)
                {
                    return;
                }

                float2 floaterPos = _FloaterData[index].xy;
                float radius = max(_FloaterData[index].z, 1e-4);
                float t = saturate(length(posXZ - floaterPos) / radius);
                float falloff = (1.0 - t) * (1.0 - t);

                foam = max(foam, falloff * strength);
            }

            float EvaluateFoam(float2 posXZ)
            {
                float foam = 0.0;

                [unroll]
                for (int i = 0; i < FLOATER_MAX; i++)
                {
                    AccumulateFoam(i, posXZ, foam);
                }

                return foam;
            }

            void EvaluateWaves(float2 posXZ, out float height, out float2 slope)
            {
                height = 0.0;
                slope = float2(0.0, 0.0);

                [unroll]
                for (int i = 0; i < RIPPLE_MAX; i++)
                {
                    AccumulateRipple(i, posXZ, height, slope);
                }
            }

            Varyings Vertex(Attributes input)
            {
                Varyings output;

                float height;
                float2 slope;
                EvaluateWaves(input.positionOS.xz, height, slope);

                float3 displacedOS = input.positionOS.xyz + float3(0.0, height, 0.0);

                output.positionWS = TransformObjectToWorld(displacedOS);
                output.positionHCS = TransformWorldToHClip(output.positionWS);

                // メッシュはオブジェクト空間で XZ 平面(Y が上)という前提で法線を組み立てる
                float3 normalOS = normalize(float3(-slope.x, 1.0, -slope.y));
                output.normalWS = TransformObjectToWorldNormal(normalOS);

                output.uv = input.uv;
                output.waveHeight = height;
                output.localXZ = input.positionOS.xz;

                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(_WorldSpaceCameraPos - input.positionWS);
                Light mainLight = GetMainLight();

                // しきい値による切り替えだと波の頂点だけ色がくっきり変わって不自然に見えるため、
                // 高さに応じてなだらかに(S字カーブで)色を混ぜる。
                float crestAmount = saturate(input.waveHeight / max(_CrestReference, 1e-4));
                float crest = smoothstep(0.0, 1.0, crestAmount);
                half3 albedo = lerp(_BaseColor.rgb, _CrestColor.rgb, crest);

                // 浮遊物の周りだけ泡立たせて、何かが浮かんでいることを見た目で伝える。
                // 中心でも水の色をうっすら透かせたいので、_FoamOpacity で頭打ちにしてから混ぜる。
                float foam = EvaluateFoam(input.localXZ) * _FoamOpacity;
                albedo = lerp(albedo, _FoamColor.rgb, foam);

                float nDotL = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = albedo * (mainLight.color * nDotL + SampleSH(normalWS));

                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float nDotH = saturate(dot(normalWS, halfDir));
                float specPower = exp2(10.0 * _Smoothness + 1.0);
                half3 specular = _SpecularColor.rgb * mainLight.color.rgb * (pow(nDotH, specPower) * _Smoothness);

                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);
                half3 rim = _FresnelColor.rgb * fresnel;

                return half4(diffuse + specular + rim, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Lit"
}
