Shader "MixVerse/KartGroundCracks"
{
    Properties
    {
        _Age ("Age", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Offset -1, -1
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; };
            CBUFFER_START(UnityPerMaterial)
                float _Age;
            CBUFFER_END
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                float fade = 1.0 - smoothstep(3.8, 5.0, _Age);
                return half4(input.color.rgb, input.color.a * fade);
            }
            ENDHLSL
        }
    }
}
