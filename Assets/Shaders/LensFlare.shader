Shader "LensFlare"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity", Float) = 1
        _AxisDistance ("Axis Distance", Float) = 0
        _RadialDistortion ("Radial Distortion", Float) = 0
        _AutoRotate ("Auto Rotate", Integer) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }

        Blend SrcAlpha One // additive blending
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
                float _Intensity;
                float _AxisDistance;
                float _RadialDistortion;
                int _AutoRotate;
            CBUFFER_END
 
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };
 
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD1;
                float4 color      : COLOR;
            };
 
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                float aspect = _ScreenParams.y / _ScreenParams.x;

                float3x3 rotScale = (float3x3)unity_ObjectToWorld;
                float3 vertCS = mul(rotScale, IN.positionOS.xyz);

                float3 objectCS = TransformObjectToHClip(float3(0, 0, 0)).xyz; // object origin in clip space
                
                float scale = 1;
                if (_RadialDistortion > 0.0)
                    scale = length(objectCS.xy) * _AxisDistance * _RadialDistortion;

                if (_AutoRotate) {
                    float2 radial = -normalize(objectCS.xy);
                    float2x2 rotate = float2x2(radial, -radial.y, radial.x);
                    vertCS.xy = mul(rotate, vertCS.xy);
                }
                
                objectCS.xy *= 1.0 - _AxisDistance;
                vertCS *= float3(1, -1 / aspect, 1);

                OUT.positionCS = float4(objectCS + scale * vertCS, 1);
                OUT.positionCS.w = 1;
                
                OUT.uv = IN.uv;
                OUT.color = float4(IN.color.rgb, IN.color.a / (scale * scale));

                return OUT;
            }
 
            float4 frag(Varyings IN) : SV_Target
            {
                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                return baseTex * _Intensity * _Color * IN.color;
            }

            ENDHLSL
        }
    }
}
