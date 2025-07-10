Shader "SOI"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,0.1)
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
                float4 _Color;
            CBUFFER_END
            
            struct Attributes
            {
                float4 positionOS : POSITION;
            };
 
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 positionOS : TEXCOORD0;
                float alpha : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionOS = IN.positionOS;

                // circle center in clip space and screen space
                float2 centerSS = UNITY_MATRIX_MVP._14_24 * _ScreenParams.xy;

                // screen space circle radius
                float radSS = length(UNITY_MATRIX_MVP._11_21 * 0.5 * _ScreenParams.xy);

                // half the size of the largest screen dimension
                float halfScreen = max(_ScreenParams.x, _ScreenParams.y);

                // sample distance from center at screen corners
                float2 samples[4] = {
                    _ScreenParams.xy,
                    float2(-_ScreenParams.x, _ScreenParams.y),
                    -_ScreenParams.xy,
                    float2(_ScreenParams.x, -_ScreenParams.y),
                };
                float4 dist = float4(0,0,0,0);
                for (int i = 0; i < 4; i++)
                    dist[i] = distance(samples[i], centerSS);
                float a = 0.25 * (dist.x+dist.y+dist.z+dist.w);
                a /= radSS; // normalize to radius
                a = (a - 0.8) / 0.4; // [0.8, 1.2] -> [0, 1]
                OUT.alpha = saturate(a);

                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float r2 = IN.positionOS.x*IN.positionOS.x + IN.positionOS.y*IN.positionOS.y;
                if (r2 > 0.25) return float4(0,0,0,0);
                else return float4(_Color.rgb, _Color.a * IN.alpha);
            }

            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
