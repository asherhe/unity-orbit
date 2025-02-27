Shader "FixedSizeIcon"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Size ("Size (px)", Float) = 32 // how many pixels each unit should translate to
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
            
            CBUFFER_START(UnityPerMaterial)
                TEXTURE2D(_MainTex);
                SAMPLER(sampler_MainTex);
                float4 _Color;
                float _Size;
            CBUFFER_END
 
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };
 
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD1;
            };
 
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                float2 screenSize = _ScreenParams.xy;
                float aspect = screenSize.y / screenSize.x;
                float pxSize = 2 / screenSize.x; // size of one horizontal pixel in clip space

                float3x3 rotScale = (float3x3)unity_ObjectToWorld;
                float3 displacement = _Size * float3(pxSize, pxSize / aspect, 1) * mul(rotScale, IN.positionOS.xyz);
                displacement.y = -displacement.y; // no clue why we do this but it doesn't work without it

                OUT.positionCS = TransformObjectToHClip(float3(0, 0, 0)) + float4(displacement, 1);
                OUT.positionCS.w = 1;
                OUT.uv = IN.uv;
                return OUT;
            }
 
            float4 frag(Varyings IN) : SV_Target
            {
                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                return baseTex * _Color;
            }
 
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
