Shader "Trajectory"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,0.2)
        [Toggle] _DoCycle ("Cycle effect?", Integer) = 1
        _CyclePeriod ("Cycle Period (s)", Float) = 5
        _CycleAlphaLow ("Cycle alpha: Low", Range(0.0, 1.0)) = 0.2
        _CycleAlphaHigh ("Cycle alpha: High", Range(0.0, 1.0)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
           
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                int _DoCycle;
                float _CyclePeriod;
                float _CycleAlphaLow;
                float _CycleAlphaHigh;
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
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }
         
            float4 frag(Varyings IN) : SV_Target
            {
                if (_DoCycle){
                    float cyclePos = frac(_Time.y / _CyclePeriod);
                    float dist = frac(IN.uv.x - cyclePos);
                    return float4(_Color.xyz, lerp(_CycleAlphaLow, _CycleAlphaHigh, dist));
                } else {
                    return _Color;
                }
            }
         
            ENDHLSL
        }
    }
}
