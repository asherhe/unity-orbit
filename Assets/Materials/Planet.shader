Shader "Planet"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _SpecularTex ("Specular", 2D) = "black" {}
        _AmbientColor ("Ambient Color", Color) = (0,0,0)
        _SunDir ("Sun Direction", Vector) = (-1,0,0.2,1)
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

           CBUFFER_START(UnityPerMaterial)
               TEXTURE2D(_MainTex);
               SAMPLER(sampler_MainTex);
               TEXTURE2D(_SpecularTex);
               SAMPLER(sampler_SpecularTex);
               float4 _AmbientColor;
               float4 _SunDir;
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
               OUT.positionOS = IN.positionOS;
               OUT.uv = IN.uv;
               return OUT;
           }

           float4 frag(Varyings IN) : SV_Target
           {
               // [-0.5, 0.5] => [-1, 1]
               IN.positionOS *= 2;

               float r2 = IN.positionOS.x*IN.positionOS.x + IN.positionOS.y*IN.positionOS.y;
               if (r2 > 1) return float4(0,0,0,0);
               float3 normal = float3(IN.positionOS.xy, -sqrt(1-r2));

               float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

               float3 color = (_AmbientColor * baseTex).xyz;
               if (baseTex.w > 0) {
                   // extract only rotation of transform matrix
                   float3x3 rotW2O = (float3x3)unity_WorldToObject;
                   rotW2O._11_21_31 = normalize(rotW2O._11_21_31);
                   rotW2O._12_22_32 = normalize(rotW2O._12_22_32);
                   rotW2O._13_23_33 = normalize(rotW2O._13_23_33);

                   float3 sunDir = normalize(mul(rotW2O, _SunDir));
                   float3 sunColor = float3(1,1,1);

                   color += baseTex.xyz * LightingLambert(sunColor, sunDir, normal);
                   
                   color += LightingSpecular(
                       sunColor,
                       sunDir,
                       normal,
                       float3(0, 0, -1),
                       SAMPLE_TEXTURE2D(_SpecularTex, sampler_SpecularTex, IN.uv),
                       20
                   );
               }

               return float4(color, baseTex.w);
           }

           ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
