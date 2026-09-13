Shader "Unlit/BloodFlowShaderURP"
{
    // 血の状態テクスチャを 1 フレーム進めるパス。ピンポンして毎フレーム呼ぶ。
    // チャンネルの意味は BloodStampShaderURP のコメントを参照。
    //
    // 流れの作り方は「上のピクセルから流れ込んできた量を集める」方式。
    // 各ピクセルは自分の血の量に応じた速さ（1 フレームに進むテクセル数）を持ち、
    // その速さぶん下へ運ばれる。三角形の重みで分配しているので血の総量はほぼ保存され、
    // 重い塊だけが先に走って細い筋を置き去りにする ＝ 垂れた跡になる。
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

            // 1 フレームで進める最大テクセル数。速さはこの値で頭打ちになる
            #define TAP_COUNT 8

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

            float4 _TexelSize;
            float _DeltaTime;
            float _Gravity;
            float _FilmThickness;
            float _SurfaceTension;
            float _TensionSoftness;
            float _DryFriction;
            float _LaneThreshold;
            float _TrailGain;
            float _DryRate;
            float _DryToStain;
            float _AgeRate;
            float _Wander;
            float _LaneScale;
            float _WanderScale;
            float _MaxThickness;

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
            /// 縦筋ごとの流れやすさ。壁の凹凸に見立てて、一部の列だけが流れるようにする。
            /// 全部の列が流れると、しぶき全体が板のようにずり落ちてしまう。
            /// </summary>
            float LaneSpeed(float x)
            {
                float lane = ValueNoise(float2(x * _LaneScale, 0.0));
                return smoothstep(_LaneThreshold, _LaneThreshold + 0.37, lane);
            }

            /// <summary>壁に貼り付いたまま動かない膜の厚み。垂れた跡はこれが残ったもの。</summary>
            float MobileAmount(float wet)
            {
                return max(wet - _FilmThickness, 0.0);
            }

            /// <summary>
            /// 1 フレームに進むテクセル数。below には 1 テクセル下の状態を渡す。
            /// 動き出したらほぼ一定の速さにして、血の玉がばらけて薄く伸びるのを防いでいる。
            /// </summary>
            float FlowSpeed(half4 state, half4 below, float x)
            {
                float drive = smoothstep(0.0, max(_TensionSoftness, 1e-4), MobileAmount(state.r) - _SurfaceTension);

                // 乾いた面へ進もうとする先端はブレーキがかかる。ここに血が溜まって玉になる
                float friction = lerp(_DryFriction, 1.0, saturate(below.r / max(_FilmThickness, 1e-4)));

                float speed = drive * friction * _Gravity * LaneSpeed(x) * (0.55 + 0.9 * state.a) * _DeltaTime;
                return min(speed, (float)TAP_COUNT);
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
                float2 uv = input.uv;
                half4 self = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                // 真下へ落ちるだけだと定規で引いたようになるので、横へ少しずつ逸らす
                float drift = (ValueNoise(float2(uv.x * _LaneScale * 0.5, uv.y * _WanderScale)) - 0.5)
                    * _Wander * _TexelSize.x;

                half4 belowSelf = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(0.0, _TexelSize.y));

                float ownSpeed = FlowSpeed(self, belowSelf, uv.x);
                float ownWeight = saturate(1.0 - ownSpeed);

                // 膜として貼り付くぶんは動かない。動くのはそれを超えた量だけ
                float ownMobile = MobileAmount(self.r);

                float wet = (self.r - ownMobile) + ownMobile * ownWeight;
                float seedSum = self.a * ownMobile * ownWeight;
                float ageSum = self.b * ownMobile * ownWeight;

                half4 below = self;

                [unroll]
                for (int i = 1; i <= TAP_COUNT; i++)
                {
                    float2 tapUV = uv + float2(drift * i, _TexelSize.y * i);
                    half4 source = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, tapUV);

                    // 速さ i のぶんだけ真下に届く。i の前後へ三角形に分配して滑らかにつなぐ
                    float weight = saturate(1.0 - abs(FlowSpeed(source, below, tapUV.x) - i));

                    // 上端の外はクランプで同じ行が返ってくるため、血が湧いてしまわないよう捨てる
                    weight *= step(tapUV.y, 1.0);

                    float mass = MobileAmount(source.r) * weight;
                    wet += mass;
                    seedSum += source.a * mass;
                    ageSum += source.b * mass;

                    below = source;
                }

                // 動いた血の量で重み付けして、しぶきごとの乱数と乾き具合も一緒に運ぶ
                float movedMass = max(wet - (self.r - ownMobile), 0.0);
                float seed = movedMass > 1e-4 ? saturate(seedSum / movedMass) : self.a;
                float age = movedMass > 1e-4 ? saturate(ageSum / movedMass) : self.b;

                // 血があったところには薄い跡が残る。垂れた筋はこれで描かれる
                float stain = max(self.g, saturate(self.r * _TrailGain));

                // 乾いたぶんは動かない染みへ移る。垂れが途中で止まるのもこれが効く
                float dried = wet * saturate(_DryRate * _DeltaTime);
                wet = min(wet - dried, _MaxThickness);
                stain = saturate(stain + dried * _DryToStain);

                // 厚い血だまりほど乾くのが遅い
                age = saturate(age + _AgeRate * _DeltaTime / (1.0 + wet * 2.0));

                return half4(wet, stain, age, seed);
            }
            ENDHLSL
        }
    }
}
