Shader "Atmosphere"
{
    Properties
    {
        _SunDir ("Sun Direction", Vector) = (-1,0,-0.2,1)
        _SunIntensity ("Sun Intensity", Float) = 20
        [Space]
        _PlanetRad ("Planet Radius (m)", Float) = 6371000
        _AtmHeight ("Atmosphere Height (m)", Float) = 100000
        _AtmSeaLevelPressure ("Sea Level Pressure (atm)", Float) = 1
        _RayleighScaleHeight ("Rayleigh Scattering Scale Height (m)", Float) = 8500
        _MieScaleHeight ("Mie Scattering Scale Height (m)", Float) = 1200
        [Space]
        _RayleighScatteringCoeff ("Rayleigh Scattering Coefficient (m^-1)", Vector) = (0.000005804542996261093, 0.000013562911419845635, 0.00003026590629238531, 0.000012619774364741572)
        _MieScatteringCoeff ("Mie Scattering Coefficient (m^-1)", Vector) = (0.0000071, 0.0000071, 0.0000071,0.0000071)
        _MiePhaseG ("Mie Phase Asymmetry", Float) = 0.6
        _ViewSamples ("Out-scattering samples", Range(0, 256)) = 24
        _LightSamples ("In-scattering samples", Range(0, 256)) = 8
    }
    // scattering pass
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        
        Cull Off
        ZWrite Off

        LOD 100

        // attenuation pass
        /*Pass
        {
            Blend Zero SrcColor // multiplicative blending
            // Blend Zero One

            HLSLPROGRAM

            #pragma vertex vert_centeredWS // see WorldSpaceVert.hlsl
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "WorldSpaceVert.hlsl"
            #include "Scattering.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
                float4 _SunDir;
                float _SunIntensity;
                float _PlanetRad;
                float _AtmHeight;
                float _AtmSeaLevelPressure;
                float _RayleighScaleHeight;
                float _MieScaleHeight;
                float4 _RayleighScatteringCoeff;
                float4 _MieScatteringCoeff;
                float _MiePhaseG;
                int _ViewSamples;
                int _LightSamples;
            CBUFFER_END

            float4 frag(WSVVaryings IN) : SV_Target
            {
                float3 attenuation = scatterAttenuation(
                    IN.position.xyz,
                    normalize(_SunDir.xyz),
                    _PlanetRad,
                    _AtmHeight,
                    _RayleighScaleHeight,
                    _MieScaleHeight,
                    _RayleighScatteringCoeff,
                    _MieScatteringCoeff,
                    _MiePhaseG,
                    _LightSamples,
                    true
                ) * _AtmSeaLevelPressure;

                return float4(attenuation, 1);
            }

            ENDHLSL
        }*/

        // scattering pass
        Pass
        {
            // Blend One One // additive blending
            Blend One SrcAlpha // add rgb and attenuate background by alpha

            HLSLPROGRAM

            #pragma vertex vert_centeredWS // see WorldSpaceVert.hlsl
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "WorldSpaceVert.hlsl"
            #include "Scattering.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
                float4 _SunDir;
                float _SunIntensity;
                float _PlanetRad;
                float _AtmHeight;
                float _AtmSeaLevelPressure;
                float _RayleighScaleHeight;
                float _MieScaleHeight;
                float4 _RayleighScatteringCoeff;
                float4 _MieScatteringCoeff;
                float _MiePhaseG;
                int _ViewSamples;
                int _LightSamples;
            CBUFFER_END

            float4 frag(WSVVaryings IN) : SV_Target
            {
                float3 L = normalize(_SunDir.xyz);

                // extra light we gain from in-scattering
                float4 scatter = scatter2D(
                    IN.position.xy,
                    L,
                    _PlanetRad,
                    _AtmHeight,
                    _RayleighScaleHeight,
                    _MieScaleHeight,
                    _RayleighScatteringCoeff,
                    _MieScatteringCoeff,
                    _MiePhaseG,
                    int2(_ViewSamples, _LightSamples)
                );
                scatter.rgb *= _SunIntensity * _AtmSeaLevelPressure;

                return scatter;
            }

            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
