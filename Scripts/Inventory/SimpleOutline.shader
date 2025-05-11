Shader "URP/OutlineUnlit"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,0,1)
        _OutlineWidth ("Outline Width", Float) = 0.05
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "UniversalForward" }
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            half4 _OutlineColor;
            float _OutlineWidth;

            Varyings vert (Attributes input)
            {
                Varyings output;
                float3 normWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz) + normWS * _OutlineWidth;
                output.positionHCS = TransformWorldToHClip(posWS);
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
