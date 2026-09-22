Shader "MixVerse/KartExplosionParticle"
{
    Properties
    {
        _Ring ("Ring", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha [_DstBlend]
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
            CBUFFER_START(UnityPerMaterial)
                float _Ring;
                float _DstBlend;
            CBUFFER_END
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                float radius = length(input.uv * 2.0 - 1.0);
                float glow = pow(saturate(1.0 - radius), 1.6);
                float ring = smoothstep(0.72, 0.82, radius) * (1.0 - smoothstep(0.85, 1.0, radius));
                return half4(input.color.rgb, input.color.a * lerp(glow, ring, _Ring));
            }
            ENDHLSL
        }
    }
}
