Shader "Unlit/BloodDisplayShaderURP"
{
    // 血の状態テクスチャを画面へ出すパス。RawImage に貼って使う。
    // 状態テクスチャは RawImage の texture として _MainTex に入る。
    // チャンネルの意味は BloodStampShaderURP のコメントを参照。
    Properties
    {
        [MainTexture] _MainTex ("State", 2D) = "black" {}

        [Header(Color)]
        _FreshColor ("Fresh (thin)", Color) = (0.49, 0.07, 0.15, 1)
        _DeepColor ("Deep (thick)", Color) = (0.08, 0.02, 0.04, 1)
        _DriedColor ("Dried", Color) = (0.18, 0.05, 0.04, 1)
        _DarkenGain ("Darken by thickness", Range(0.1, 6)) = 1.6
        _DryTint ("Dried tint", Range(0, 1)) = 0.85

        [Header(Coverage)]
        _StainThickness ("Stain thickness", Range(0, 2)) = 0.55
        _CoverageGain ("Coverage gain", Range(0.5, 20)) = 5
        _Opacity ("Opacity", Range(0, 1)) = 0.95

        [Header(Wetness)]
        _SpecularIntensity ("Specular", Range(0, 2)) = 0.35
        _Gloss ("Gloss", Range(1, 256)) = 42
        _NormalStrength ("Normal strength", Range(0.05, 4)) = 0.7
        _LightDirection ("Light direction", Vector) = (-0.5, 0.75, 0.65, 0)
        _Shading ("Shading", Range(0, 1)) = 0.35
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
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // テクスチャは RawImage 側から差し込まれるので、テクセルサイズは C# から渡してもらう
            float4 _StateTexelSize;

            half4 _FreshColor;
            half4 _DeepColor;
            half4 _DriedColor;
            float _DarkenGain;
            float _DryTint;
            float _StainThickness;
            float _CoverageGain;
            float _Opacity;
            float _SpecularIntensity;
            float _Gloss;
            float _NormalStrength;
            float4 _LightDirection;
            float _Shading;

            /// <summary>濡れた血と乾いた染みを合わせた厚み。見た目はほぼこれで決まる。</summary>
            float Thickness(float2 uv)
            {
                half4 state = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                return state.r + state.g * _StainThickness;
            }

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
                half4 state = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                float wet = state.r;
                float age = state.b;
                float thickness = wet + state.g * _StainThickness;

                if (thickness <= 0.0)
                {
                    return half4(0, 0, 0, 0);
                }

                // 厚いほど光を通さなくなる。薄い縁だけがワインレッドに透けて見える
                float alpha = 1.0 - exp(-thickness * _CoverageGain);
                float depth = 1.0 - exp(-thickness * _DarkenGain);

                half3 color = lerp(_FreshColor.rgb, _DeepColor.rgb, depth);
                color = lerp(color, _DriedColor.rgb, age * _DryTint);

                // 厚みの傾きを法線とみなして、盛り上がりと濡れた照りを作る
                float2 texel = _StateTexelSize.xy;
                float left = Thickness(input.uv - float2(texel.x, 0));
                float right = Thickness(input.uv + float2(texel.x, 0));
                float down = Thickness(input.uv - float2(0, texel.y));
                float up = Thickness(input.uv + float2(0, texel.y));

                float3 normal = normalize(float3(left - right, down - up, _NormalStrength));
                float3 lightDirection = normalize(_LightDirection.xyz);
                float3 halfVector = normalize(lightDirection + float3(0, 0, 1));

                float diffuse = saturate(dot(normal, lightDirection));
                color *= lerp(1.0, 0.55 + diffuse, _Shading);

                // 乾くほどつやが引いていく
                float wetness = saturate(wet * 2.0) * (1.0 - age);
                float specular = pow(saturate(dot(normal, halfVector)), _Gloss) * _SpecularIntensity * wetness;
                color += specular;

                return half4(color, saturate(alpha) * _Opacity) * input.color;
            }
            ENDHLSL
        }
    }
}
