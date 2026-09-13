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

        // はがれ始める角（UV）。既定は左上。
        _PeelOrigin ("Peel Origin (UV)", Vector) = (0, 1, 0, 0)
        // はがれ始める角から対角の終点までの向きと距離（UV 空間、正規化しない）。既定は右下へ。
        _PeelDirection ("Peel Direction (UV)", Vector) = (1, -1, 0, 0)

        [Header(Peeled Back Side)]
        // シールをめくった直後、紙自体の裏面（シルバーの箔など）が一瞬見えるように色を挟む
        _BackColor ("Peeled Back Color", Color) = (0.75, 0.76, 0.78, 1)
        _BackWidth ("Peeled Back Width", Range(0.001, 0.5)) = 0.12

        [Header(Edge Look)]
        _EdgeWidth ("Edge Width", Range(0.001, 0.3)) = 0.05
        _EdgeHighlightColor ("Edge Highlight Color", Color) = (1, 1, 1, 1)
        _EdgeHighlightPower ("Edge Highlight Power", Float) = 1.5

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
            float4 _BackColor;
            float _BackWidth;
            float _EdgeWidth;
            float4 _EdgeHighlightColor;
            float _EdgeHighlightPower;
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

            half4 frag (Varyings input) : SV_Target
            {
                // はがれ始めた角から、この UV が対角方向にどれだけ進んだ位置にあるかを 0〜1 で求める
                float2 toPixel = input.uv - _PeelOrigin.xy;
                float span = dot(_PeelDirection.xy, _PeelDirection.xy);
                float t = saturate(dot(toPixel, _PeelDirection.xy) / max(span, 1e-6));

                half4 topColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 bottomColor = SAMPLE_TEXTURE2D(_BottomTex, sampler_BottomTex, input.uv);

                // まだはがれていない（t が進捗より先の）部分は Top、はがれ終わった部分は Bottom
                bool peeled = t < _Progress;
                half4 baseColor = peeled ? bottomColor : topColor;

                // まだはがれていない側のうち境界のすぐ手前は、めくれた紙自体の裏（シールの裏面）を見せる
                float distanceFromBoundary = t - _Progress;
                float backMask = 1.0 - saturate(distanceFromBoundary / max(_BackWidth, 1e-4));
                baseColor.rgb = lerp(baseColor.rgb, _BackColor.rgb, peeled ? 0.0 : backMask);

                // はがれた直後の Bottom 側に影を落として、めくれた紙の下にできる陰を表す。
                // 境界から離れるほど（はがれてから時間が経つほど）薄くなる。
                float distanceIntoRevealed = _Progress - t;
                float shadow = saturate(1.0 - (distanceIntoRevealed / max(_ShadowWidth, 1e-4))) * _ShadowPower;
                baseColor.rgb = lerp(baseColor.rgb, _ShadowColor.rgb, peeled ? shadow : 0.0);

                // はがれる境界そのものに紙の縁のハイライトを乗せる
                float edge = 1.0 - saturate(abs(t - _Progress) / max(_EdgeWidth, 1e-4));
                half3 highlight = _EdgeHighlightColor.rgb * (edge * edge * _EdgeHighlightPower);

                half4 result;
                result.rgb = baseColor.rgb + highlight;
                result.a = baseColor.a;

                return result * input.color;
            }
            ENDHLSL
        }
    }
}
