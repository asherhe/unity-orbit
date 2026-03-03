Shader "Part"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        [Normal] _NormalMap ("Normal Map", 2D) = "bump" {}
        _EmissionTex ("Emission Texture", 2D) = "black" {}

        _SunDir ("Sun Direction", Vector) = (-1,0,0.2,0)
        _SunColor ("Sun Color", Color) = (1,1,1,1)
        _SunIntensity ("Sun Intensity", Float) = 2 // sun intensity based on distance from sun

        _PlanetShineColor ("Planet Shine Color", Color) = (1,1,1,1)
        _PlanetShineIntensity ("Planet Shine Intensity", Float) = 1
        _PlanetShineDir ("Planet Shine Direction", Vector) = (0,-1,0,0)
        _PlanetShineSpread ("Planet Shine Spread", Float) = 0.4
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"
            
            #pragma multi_compile_instancing
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_0 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_1 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_2 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_3 __
            #pragma multi_compile _ DEBUG_DISPLAY

            CBUFFER_START(UnityPerMaterial)
                TEXTURE2D(_MainTex);
                SAMPLER(sampler_MainTex);
                TEXTURE2D(_NormalMap);
                SAMPLER(sampler_NormalMap);
                TEXTURE2D(_EmissionTex);
                SAMPLER(sampler_EmissionTex);
                float4 _MainTex_ST;
                
                float4 _SunDir;
                float4 _SunColor;
                float _SunIntensity;

                float4 _PlanetShineColor;
                float _PlanetShineIntensity;
                float4 _PlanetShineDir;
                float _PlanetShineSpread;
            CBUFFER_END

            #if USE_SHAPE_LIGHT_TYPE_0
            SHAPE_LIGHT(0)
            #endif

            #if USE_SHAPE_LIGHT_TYPE_1
            SHAPE_LIGHT(1)
            #endif

            #if USE_SHAPE_LIGHT_TYPE_2
            SHAPE_LIGHT(2)
            #endif

            #if USE_SHAPE_LIGHT_TYPE_3
            SHAPE_LIGHT(3)
            #endif

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 lightingUV: TEXCOORD1;
            };

            Varyings vert(Attributes v)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(v.uv, _MainTex);
                OUT.lightingUV = ComputeScreenPos(OUT.positionCS / OUT.positionCS.w).xy;
                return OUT;
            }

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

            float4 frag(Varyings IN) : SV_Target
            {
                float4 main = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float3 normal = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv));
                
                float3 worldNormal = TransformObjectToWorldNormal(normal);

                // total light input
                float3 color = float3(0, 0, 0);

                // sun illumination
                color += main.rgb * LightingLambert(_SunIntensity * _SunColor.rgb, normalize(_SunDir.xyz), worldNormal);

                // planet shine
                float planetShineLambert = _PlanetShineSpread + (1-_PlanetShineSpread) * (normalize(_PlanetShineDir.xyz) * worldNormal);
                planetShineLambert = clamp(planetShineLambert, 0, 1);
                color += main.rgb * _PlanetShineIntensity * _PlanetShineColor.rgb * planetShineLambert;

                // scene lighting
                SurfaceData2D surfaceData;
                InputData2D inputData;
                InitializeSurfaceData(main.rgb, main.a, surfaceData);
                InitializeInputData(IN.uv, IN.lightingUV, inputData);
                color += CombinedShapeLightShared(surfaceData, inputData).rgb;

                float4 shaded = float4(color, main.a);

                float4 emission = SAMPLE_TEXTURE2D(_EmissionTex, sampler_EmissionTex, IN.uv);
                // overlay rgba of emission onto shaded
                float aout = emission.a + shaded.a * (1 - emission.a);
                shaded.rgb = (emission.rgb * emission.a + shaded.rgb * shaded.a * (1 - emission.a)) / aout;
                shaded.a = aout;

                return shaded;
            }

            ENDHLSL
        }

        Pass
        {
            // based on Packages/com.unity.render-pipelines.universal/Shaders/2D/Sprite-Lit-Default.shader

            Tags { "LightMode" = "NormalsRendering" }
            
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex NormalsRenderingVertex
            #pragma fragment NormalsRenderingFragment

            // GPU Instancing
            #pragma multi_compile_instancing

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                float4 tangent      : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS      : SV_POSITION;
                half4   color           : COLOR;
                float2  uv              : TEXCOORD0;
                half3   normalWS        : TEXCOORD1;
                half3   tangentWS       : TEXCOORD2;
                half3   bitangentWS     : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            half4 _NormalMap_ST;  // Is this the right way to do this?

            Varyings NormalsRenderingVertex(Attributes attributes)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(attributes);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

#ifdef UNITY_INSTANCING_ENABLED
                attributes.positionOS = UnityFlipSprite(attributes.positionOS, unity_SpriteFlip);
#endif
                o.positionCS = TransformObjectToHClip(attributes.positionOS);
                o.uv = TRANSFORM_TEX(attributes.uv, _NormalMap);
                o.color = attributes.color;
                o.normalWS = -GetViewForwardDir();
                o.tangentWS = TransformObjectToWorldDir(attributes.tangent.xyz);
                o.bitangentWS = cross(o.normalWS, o.tangentWS) * attributes.tangent.w;
#ifdef UNITY_INSTANCING_ENABLED
                o.color *= unity_SpriteColor;
#endif
                return o;
            }

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl"

            half4 NormalsRenderingFragment(Varyings i) : SV_Target
            {
                const half4 mainTex = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                const half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uv));

                return NormalsRenderingShared(mainTex, normalTS, i.tangentWS.xyz, i.bitangentWS.xyz, i.normalWS.xyz);
            }
            ENDHLSL
        }
    }
}
