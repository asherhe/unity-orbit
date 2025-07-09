Shader "PlanetBlank"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _AmbientColor ("Ambient Color", Color) = (0.08,0.1,0.1)
        _SunDir ("Sun Direction", Vector) = (-1,0,0.2,0)
        _SunIntensity ("Sun Intensity", Float) = 20
        _PlanetRad ("Planet Radius (m)", Float) = 6371000
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

            #pragma vertex vert_centeredWS
            #pragma fragment frag
           
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "WorldSpaceVert.hlsl"
            #include "Scattering.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _AmbientColor;
                float4 _SunDir;
                float _SunIntensity;
                float _PlanetRad;
            CBUFFER_END
            
            float4 frag(WSVVaryings IN) : SV_Target
            {
                float3 L = normalize((float3)_SunDir);

                IN.position /= _PlanetRad;

                float r2 = IN.position.x*IN.position.x + IN.position.y*IN.position.y;
                if (r2 > 1) return float4(0,0,0,0);

                float3 normal = float3(IN.position.xy, -sqrt(1-r2));
                
                float3 color =
                    _AmbientColor.rgb +
                    LightingLambert(float3(1,1,1), L, normal);
                color *= _Color.rgb * _SunIntensity;
                return float4(color, 1);
            }

            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
