Shader "Custom/StylizedCreatureLitURP"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)
        _ShadowStrength ("Shadow Strength", Range(0,1)) = 0.4
        _RimColor ("Rim Color", Color) = (0.2,0.2,0.2,1)
        _RimPower ("Rim Power", Range(1,8)) = 4
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap); float4 _BaseMap_ST;
            float4 _Color;
            float _ShadowStrength;
            float4 _RimColor;
            float _RimPower;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 worldPos    : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normal = normalize(IN.normalWS);
                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.worldPos);

                // Sample texture
                float4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _Color;

                // Lighting
                Light light = GetMainLight();
                float NdotL = saturate(dot(normal, light.direction));
                float shadowFactor = 1.0 - light.shadowAttenuation;
                float lit = lerp(1.0, NdotL, _ShadowStrength);

                // Rim light (optional)
                float rim = pow(1.0 - saturate(dot(normal, viewDir)), _RimPower);
                float3 rimLight = rim * _RimColor.rgb;

                float3 finalColor = baseColor.rgb * lit + rimLight;

                return float4(finalColor, baseColor.a);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/InternalErrorShader"
}
