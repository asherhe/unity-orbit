Shader "Planet"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _SpecularTex ("Specular", 2D) = "black" {}
        [Normal] _NormalMap ("Normal map", 2D) = "bump" {}
        _LightTex ("Lights", 2D) = "black" {}
        _LightColor ("Light Color", Color) = (1,1,1,0.2)
        _AmbientColor ("Ambient Color", Color) = (0.08,0.1,0.1)
        // sun & atmosphere settings
        _SunDir ("Sun Direction", Vector) = (-1,0,0.2,1)
        _SunIntensity ("Sun Intensity", Float) = 20
        _PlanetRad ("Planet Radius (m)", Float) = 6371000
        _AtmHeight ("Atmosphere Height (m)", Float) = 100000
        _AtmSeaLevelPressure ("Sea Level Pressure (atm)", Float) = 1
        _AtmScaleHeight ("Atmosphere Scale Height (m)", Float) = 8500
        _RayleighScatteringCoeff ("Rayleigh Scattering Coefficient (m^-1)", Vector) = (0.000005804542996261093, 0.000013562911419845635, 0.00003026590629238531, 0.000012619774364741572)
        _LightSamples ("In-scattering samples", Range(0, 256)) = 24
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        LOD 100

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
           
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Scattering.hlsl"

            CBUFFER_START(UnityPerMaterial)
                TEXTURE2D(_MainTex);
                SAMPLER(sampler_MainTex);
                TEXTURE2D(_SpecularTex);
                SAMPLER(sampler_SpecularTex);
                TEXTURE2D(_NormalMap);
                SAMPLER(sampler_NormalMap);
                TEXTURE2D(_LightTex);
                SAMPLER(sampler_LightTex);
                float4 _LightColor;
                float4 _AmbientColor;
                
                float4 _SunDir;
                float _SunIntensity;
                float _PlanetRad;
                float _AtmHeight;
                float _AtmSeaLevelPressure;
                float _AtmScaleHeight;
                float4 _RayleighScatteringCoeff;
                int _ViewSamples;
                int _LightSamples;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 positionOS : TEXCOORD0;
                float2 uv         : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                
                // extract only rotation of transform matrix
                float3x3 rotO2W = (float3x3)unity_ObjectToWorld;
                rotO2W._11_21_31 = normalize(rotO2W._11_21_31);
                rotO2W._12_22_32 = normalize(rotO2W._12_22_32);
                rotO2W._13_23_33 = normalize(rotO2W._13_23_33);
                OUT.positionOS = float4(mul(rotO2W, (float3)IN.positionOS), 1);

                OUT.uv = IN.uv;

                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float3 L = normalize((float3)_SunDir);

                float r2 = IN.positionOS.x*IN.positionOS.x + IN.positionOS.y*IN.positionOS.y;
                if (r2 > 1) return float4(0,0,0,0);

                // surface normal if the planet was a perfectly smooth sphere
                float3 sphereNormal = float3(IN.positionOS.xy, -sqrt(1-r2));
                
                float3 normalMap = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv)).xyz;
                
                float3x3 rotO2W = (float3x3)unity_ObjectToWorld;
                rotO2W._11_21_31 = normalize(rotO2W._11_21_31);
                rotO2W._12_22_32 = normalize(rotO2W._12_22_32);
                rotO2W._13_23_33 = normalize(rotO2W._13_23_33);
                normalMap = mul(rotO2W, normalMap);

                float3 nz = sphereNormal;
                float3 nx = normalize(float3(-nz.z, 0, nz.x));
                float3 ny = cross(nx, nz);
                float3 normal = normalize(nx*normalMap.x + ny*normalMap.y + nz*normalMap.z);
                
                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                float3 color = (_AmbientColor * baseTex).rgb;
                float opticalDepth;
                bool overground = lightSampling(
                    _PlanetRad * sphereNormal,
                    L,
                    _PlanetRad,
                    _AtmHeight,
                    _AtmScaleHeight,
                    _LightSamples,
                    opticalDepth
                );
                float3 sunColor = float3(0, 0, 0);
                if (overground) {
                    sunColor = exp(-_RayleighScatteringCoeff.rgb * opticalDepth);
                }

                color += baseTex.rgb * LightingLambert(sunColor, L, normal);

                color += LightingSpecular(
                    sunColor,
                    L,
                    normal,
                    float3(0, 0, -1),
                    SAMPLE_TEXTURE2D(_SpecularTex, sampler_SpecularTex, IN.uv),
                    20
                );

                float sunAngle = dot(sphereNormal, L);
                if (sunAngle < 0) {
                    float3 lightTex = (float3)SAMPLE_TEXTURE2D(_LightTex, sampler_LightTex, IN.uv);
                    float fade = clamp(-4 * sunAngle, 0, 1); // fade lights to full brightness
                    color += lightTex * _LightColor.rgb * _LightColor.a * fade;
                }

                return float4(color, 1);
            }

            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
