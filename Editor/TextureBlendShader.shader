Shader "ProBuilder/Diffuse Texture Blend Lit"
{
    Properties
    {
        _FirstTex ("Texture", 2D) = "white" {}
        _SecondTex ("Texture", 2D) = "white" {}
        _ThirdTex ("Texture", 2D) = "white" {}
        _FourthTex ("Texture", 2D) = "white" {}
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
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_FirstTex);  SAMPLER(sampler_FirstTex);  float4 _FirstTex_ST;
            TEXTURE2D(_SecondTex); SAMPLER(sampler_SecondTex); float4 _SecondTex_ST;
            TEXTURE2D(_ThirdTex);  SAMPLER(sampler_ThirdTex);  float4 _ThirdTex_ST;
            TEXTURE2D(_FourthTex); SAMPLER(sampler_FourthTex); float4 _FourthTex_ST;

            float _ShadowStrength;
            float4 _RimColor;
            float _RimPower;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos    : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
                float4 color       : COLOR;
                float4 shadowCoord : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionHCS = TransformWorldToHClip(OUT.worldPos);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                OUT.shadowCoord = TransformWorldToShadowCoord(OUT.worldPos);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv0 = IN.uv * _FirstTex_ST.xy + _FirstTex_ST.zw;
                float2 uv1 = IN.uv * _SecondTex_ST.xy + _SecondTex_ST.zw;
                float2 uv2 = IN.uv * _ThirdTex_ST.xy + _ThirdTex_ST.zw;
                float2 uv3 = IN.uv * _FourthTex_ST.xy + _FourthTex_ST.zw;

                half4 c0 = SAMPLE_TEXTURE2D(_FirstTex, sampler_FirstTex, uv0);
                half4 c1 = SAMPLE_TEXTURE2D(_SecondTex, sampler_SecondTex, uv1);
                half4 c2 = SAMPLE_TEXTURE2D(_ThirdTex, sampler_ThirdTex, uv2);
                half4 c3 = SAMPLE_TEXTURE2D(_FourthTex, sampler_FourthTex, uv3);

                half4 blend = normalize(IN.color);
                half3 albedo = c0.rgb * blend.r;
                albedo = lerp(albedo, c1.rgb, blend.g);
                albedo = lerp(albedo, c2.rgb, blend.b);
                albedo = lerp(albedo, c3.rgb, blend.a);

                float3 normalWS = normalize(IN.normalWS);
                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.worldPos);

                Light light = GetMainLight(IN.shadowCoord);
                float shadowAtt = light.shadowAttenuation;
                float NdotL = saturate(dot(normalWS, light.direction));
                float lit = lerp(1.0, NdotL, _ShadowStrength);

                // Rim lighting
                float rim = pow(1.0 - saturate(dot(normalWS, viewDir)), _RimPower);
                float3 rimLight = rim * _RimColor.rgb;

                float3 lighting = albedo * light.color * lit * shadowAtt + rimLight;

                return float4(lighting, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/InternalErrorShader"
}
