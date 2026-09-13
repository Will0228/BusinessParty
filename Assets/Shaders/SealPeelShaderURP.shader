Shader "Unlit/SealPeelShaderURP"
{
    Properties
    {
        // はがれる側（例：ObjectGroupA を撮ったスナップショット）。
        // RawImage が内部で _MainTex を要求するため、Top 側をこの名前にしている。
        [MainTexture] _MainTex ("Top Texture (Peeling)", 2D) = "white" {}
        // はがした後に見える側（例：ObjectGroupB を撮ったスナップショット）
        _BottomTex ("Bottom Texture (Revealed)", 2D) = "white" {}

        // 0 = Top がそのまま見えている / 1 = 完全にはがれて Bottom だけになる
        _Progress ("Progress", Range(0, 1)) = 0

        [Header(Peel Shape)]
        // はがれ始める角（UV）。既定は左上。
        _PeelOrigin ("Peel Origin (UV)", Vector) = (0, 1, 0, 0)
        // はがれ始める角から対角の終点までの向きと距離（UV 空間、正規化しない）。既定は右下へ。
        _PeelDirection ("Peel Direction (UV)", Vector) = (1, -1, 0, 0)
        // 境界線の丸み。1 で角から広がる自然な円弧、大きいほど直線的な対角カットに近づき、
        // 小さいほど角から尖った舌状に広がる。
        _PeelSpread ("Peel Spread", Range(0.3, 3)) = 1

        [Header(Curl Look)]
        // はがれ際で紙が巻き上がって見える帯の太さ（Progress と同じ UV スケール）。
        _BackWidth ("Curl Width", Range(0.001, 0.5)) = 0.12
        // 巻き上がった紙の裏面（シルバーの箔など）の色。
        _BackColor ("Peeled Back Color", Color) = (0.75, 0.76, 0.78, 1)

        [Header(Edge Look)]
        // 巻き上がりの頂点（真横から見える位置）にできるハイライトの鋭さ。大きいほど細く強い筋になる。
        _EdgeHighlightPower ("Edge Highlight Sharpness", Float) = 3
        // ハイライトの強さ。
        _EdgeHighlightIntensity ("Edge Highlight Intensity", Range(0, 3)) = 0.7
        _EdgeHighlightColor ("Edge Highlight Color", Color) = (1, 1, 1, 1)

        [Header(Shadow)]
        _ShadowColor ("Peeled Shadow Color", Color) = (0, 0, 0, 1)
        _ShadowWidth ("Peeled Shadow Width", Range(0.001, 0.3)) = 0.08
        _ShadowPower ("Peeled Shadow Power", Range(0, 1)) = 0.5
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
            // UI（Canvas）上で他の要素と正しく重なるようにアルファブレンドで描く
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR; // CanvasGroup のアルファはここに乗ってくる
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_BottomTex);
            SAMPLER(sampler_BottomTex);

            float _Progress;
            float4 _PeelOrigin;
            float4 _PeelDirection;
            float _PeelSpread;
            float4 _BackColor;
            float _BackWidth;
            float4 _EdgeHighlightColor;
            float _EdgeHighlightPower;
            float _EdgeHighlightIntensity;
            float4 _ShadowColor;
            float _ShadowWidth;
            float _ShadowPower;

            Varyings vert (Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS);
                o.uv = input.uv;
                o.color = input.color;
                return o;
            }

            // はがれ始めた角からの距離を、直線ではなく楕円（≒円弧）で測る。
            // これにより、はがれる境界線が対角線一本の直線ではなく、角から丸く広がる
            // 自然なめくれのラインになる。戻り値は角で 0、_PeelDirection の先端で 1。
            float ComputePeelDistance(float2 uv)
            {
                float2 dir = _PeelDirection.xy;
                float maxAlong = max(length(dir), 1e-5);
                float2 dirN = dir / maxAlong;
                float2 perpN = float2(-dirN.y, dirN.x);

                float2 toPixel = uv - _PeelOrigin.xy;
                float along = dot(toPixel, dirN);
                float perp = dot(toPixel, perpN);

                float u = along / maxAlong;
                float v = perp / (maxAlong * max(_PeelSpread, 1e-3));

                return length(float2(u, v));
            }

            half4 frag (Varyings input) : SV_Target
            {
                float t = ComputePeelDistance(input.uv);

                half4 topColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 bottomColor = SAMPLE_TEXTURE2D(_BottomTex, sampler_BottomTex, input.uv);

                float halfBand = max(_BackWidth, 1e-4) * 0.5;
                float outerEdge = _Progress + halfBand; // ここより外側はまだ完全に貼り付いた Top
                float innerEdge = _Progress - halfBand; // ここより内側は完全にはがれ終わった Bottom

                half4 result;

                if (t >= outerEdge)
                {
                    // まだはがれていない
                    result = topColor;
                }
                else if (t <= innerEdge)
                {
                    // はがれ終わった直後は、巻き上がった紙が落とす影を落として厚みを出す
                    float distPastBand = innerEdge - t;
                    float shadow = saturate(1.0 - distPastBand / max(_ShadowWidth, 1e-4)) * _ShadowPower;
                    half3 rgb = lerp(bottomColor.rgb, _ShadowColor.rgb, shadow);
                    result = half4(rgb, bottomColor.a);
                }
                else
                {
                    // 巻き上がりの帯の中。theta = 0 で紙がまだ平らに接している側、
                    // theta = PI で完全に丸まって Bottom へ接地する側。
                    // cos(theta) を円柱の断面が視線に対してどれだけ正面/背面を向いているかに見立てて
                    // Top と裏面色を混ぜ、sin(theta) が 1 になる真横（頂点）にハイライトを乗せることで
                    // 紙が丸まっているように見せている。
                    float theta = (outerEdge - t) / max(_BackWidth, 1e-4) * PI;
                    float c = cos(theta);
                    float frontFacing = saturate(c);
                    float backFacing = saturate(-c);
                    float rim = pow(saturate(sin(theta)), max(_EdgeHighlightPower, 1e-3)) * _EdgeHighlightIntensity;

                    half3 rgb = topColor.rgb * frontFacing
                        + _BackColor.rgb * backFacing
                        + _EdgeHighlightColor.rgb * rim;
                    half a = lerp(topColor.a, bottomColor.a, backFacing);

                    result = half4(rgb, a);
                }

                return result * input.color;
            }
            ENDHLSL
        }
    }
}
