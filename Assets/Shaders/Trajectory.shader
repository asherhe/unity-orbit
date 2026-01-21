Shader "Trajectory"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,0.2)
        _Width ("Width (px)", Integer) = 4
        _MiterThreshold ("Miter Threshold", Float) = 0.8
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
            // based on https://github.com/devOdico/GoodLines/blob/master/Runtime/Assets/Shaders/Line.shader

            #pragma vertex vert
            #pragma fragment frag
           
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                int _Width;
                float _MiterThreshold;
                int _DoCycle;
                float _CyclePeriod;
                float _CycleAlphaLow;
                float _CycleAlphaHigh;
            CBUFFER_END

            struct Attributes
            {
               float4 cur  : POSITION;
               float2 uv   : TEXCOORD0;
               float2 prev : TEXCOORD1; // previous point position
               float2 next : TEXCOORD2; // next point position
               float2 data : TEXCOORD3; // data about this vertex: ( side, corner? )
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.uv = IN.uv;

                float4 cur = TransformObjectToHClip(float3(IN.cur.xy, 0));
                float4 prev = TransformObjectToHClip(float3(IN.prev.xy, 0));
                float4 next = TransformObjectToHClip(float3(IN.next.xy, 0));

                float2 cur_screen = cur.xy * _ScreenParams.xy;
                float2 prev_screen = prev.xy * _ScreenParams.xy;
                float2 next_screen = next.xy * _ScreenParams.xy;

                // direction to next point
                float2 dir = float2(0, 0);
                float len = _Width;

                if (IN.data.y == 0)
                {
                    float2 dirA = normalize(next_screen - cur_screen);
                    float2 dirB = normalize(cur_screen - prev_screen);

                    dirB *= sign(dot(dirA, dirB) + _MiterThreshold);

                    float2 tangent = (dirA + dirB) / 2;

                    float2 perp_tangent = float2(-tangent.y, tangent.x);
                    float2 perp_dirA = float2(-dirA.y, dirA.x);
                    
                    dir = tangent;
                    len /= dot(perp_tangent, perp_dirA);
                }
                else if (IN.data.y == 1)
                    dir = normalize(next_screen - cur_screen);
                else if (IN.data.y == 2)
                    dir = normalize(cur_screen - next_screen);

                float2 normal = float2(-dir.y, dir.x);
                normal *= len;
                cur_screen += normal * IN.data.x;

                OUT.positionCS = float4(cur_screen / _ScreenParams.xy, 0, 1);
                return OUT;
            }
         
            float4 frag(Varyings IN) : SV_Target
            {
                if (_DoCycle) {
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
