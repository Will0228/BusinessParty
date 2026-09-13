Shader "Unlit/BloodStampShaderURP"
{
    // 血の状態テクスチャ（RGBAHalf）に、新しい血しぶきを 1 発ぶんだけ書き足すパス。
    // 状態テクスチャのチャンネルの意味は 3 つのシェーダーで共通。
    //   R = 濡れている血の量（重いほど速く流れ落ちる）
    //   G = 乾いて残った染み（流れずにその場に残る跡）
    //   B = 経過（0 = 新鮮、1 = 乾ききってどす黒い）
    //   A = しぶきごとの乱数。流れる速さのばらつきに使う
    Properties
    {
        [MainTexture] _MainTex ("State", 2D) = "black" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define DROPLET_MAX 24

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float2 _SplatCenter;
            float2 _SplatDirection;
            float _SplatRadius;
            float _SplatAmount;
            float _SplatSeed;
            float _SplatStretch;
            float _SplatDirectionality;
            float _DropletCount;
            float _DropletSpread;
            float _ImpactStain;
            float _Aspect;

            float Hash11(float n)
            {
                return frac(sin(n * 127.1) * 43758.5453);
            }

            float2 Hash21(float n)
            {
                return frac(sin(float2(n * 127.1, n * 311.7)) * 43758.5453);
            }

            float Hash12(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = Hash12(i);
                float b = Hash12(i + float2(1.0, 0.0));
                float c = Hash12(i + float2(0.0, 1.0));
                float d = Hash12(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            /// <summary>
            /// メタボール（距離の逆二乗の足し合わせ）でしぶきの形を作る。
            /// 玉どうしが近いと勝手につながって細い橋ができるので、飛沫らしい輪郭になる。
            /// p は「進行方向が +x、1.0 = _SplatRadius」の空間。
            /// </summary>
            float SplatField(float2 p)
            {
                float angle = atan2(p.y, p.x);
                float seedTurn = _SplatSeed * 6.2831853;

                // 中心の血だまり。輪郭を角度でうねらせて円っぽさを消す
                float wobble = 0.70
                    + 0.16 * sin(angle * 3.0 + seedTurn)
                    + 0.10 * sin(angle * 5.0 - seedTurn * 2.0)
                    + 0.06 * sin(angle * 9.0 + seedTurn * 3.0);

                float field = (wobble * wobble) / max(dot(p, p), 1e-5);

                int count = min((int)_DropletCount, DROPLET_MAX);

                for (int i = 0; i < count; i++)
                {
                    float2 r1 = Hash21(_SplatSeed * 37.0 + i * 7.13);
                    float2 r2 = Hash21(_SplatSeed * 91.0 + i * 3.71);

                    // 飛び散る向きは進行方向側へ寄せる
                    float spread = lerp(3.1415926, 0.7, saturate(_SplatDirectionality));
                    float dropletAngle = (r1.x - 0.5) * 2.0 * spread;

                    float distance = lerp(0.75, 1.0 + _DropletSpread, r1.y * r1.y);
                    float2 dropletCenter = float2(cos(dropletAngle), sin(dropletAngle)) * distance;

                    // 遠くへ飛んだ粒ほど小さくなる
                    float radius = (0.10 + 0.24 * r2.x) / (0.55 + distance);

                    float2 d = p - dropletCenter;
                    field += (radius * radius) / max(dot(d, d), 1e-5);
                }

                return field;
            }

            Varyings vert (Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS);
                o.uv = input.uv;
                return o;
            }

            half4 frag (Varyings input) : SV_Target
            {
                half4 previous = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // UV は画面のアスペクト比ぶん横に伸びているので、円が円に見えるよう補正する
                float2 delta = input.uv - _SplatCenter;
                delta.x *= _Aspect;
                float2 p = delta / max(_SplatRadius, 1e-4);

                // 進行方向を +x に持ってきてから、その向きへ引き伸ばす
                float2 dir = normalize(_SplatDirection + float2(1e-5, 0.0));
                p = float2(dot(p, dir), dot(p, float2(-dir.y, dir.x)));
                p.x /= max(_SplatStretch, 1e-4);

                // 遠くのピクセルまでメタボールを回すのは無駄なので早めに切る
                if (dot(p, p) > 16.0)
                {
                    return previous;
                }

                float field = SplatField(p);
                float mask = smoothstep(0.85, 1.15, field);

                if (mask <= 0.0)
                {
                    return previous;
                }

                // 厚みをむらにしておくと、濃いところだけが垂れて薄いところは残る
                float grain = 0.55 + 0.45 * ValueNoise(input.uv * 24.0 + _SplatSeed * 53.0);
                float amount = mask * _SplatAmount * grain;

                half4 result;
                result.r = previous.r + amount;
                result.g = max(previous.g, saturate(amount * _ImpactStain));
                result.b = lerp(previous.b, 0.0, saturate(amount * 3.0));
                result.a = lerp(previous.a, Hash11(_SplatSeed * 17.0), saturate(amount * 3.0));

                return result;
            }
            ENDHLSL
        }
    }
}
