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
        _AtmScaleHeight ("Atmosphere Scale Height (m)", Float) = 8500
        [Space]
        _RayleighScatteringCoeff ("Rayleigh Scattering Coefficient (m^-1)", Vector) = (0.000005804542996261093, 0.000013562911419845635, 0.00003026590629238531, 0.000012619774364741572)
        _ViewSamples ("Out-scattering samples", Range(0, 256)) = 24
        _LightSamples ("In-scattering samples", Range(0, 256)) = 8
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        
        Blend One SrcAlpha // we use alpha channel to scatter background stuff
        Cull Off
        ZWrite Off

        LOD 100

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Scattering.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
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
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 position   : TEXCOORD0; // rotation and scale are in world space coordinates but centered at 0,0
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs positions = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);

                unity_ObjectToWorld._14_24_34 = 0;
                OUT.position = mul(unity_ObjectToWorld, IN.positionOS);

                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float4 scatter = scatter2D(
                    IN.position.xy,
                    normalize(_SunDir.xyz),
                    _RayleighScatteringCoeff,
                    _PlanetRad,
                    _AtmHeight,
                    _AtmScaleHeight,
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
