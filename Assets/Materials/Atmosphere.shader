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
        _AtmColor ("Atmosphere Color", Color) = (1,1,1,1)
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
           
           CBUFFER_START(UnityPerMaterial)
               float4 _SunDir;
               float _SunIntensity;
               float _PlanetRad;
               float _AtmHeight;
               float _AtmSeaLevelPressure;
               float _AtmScaleHeight;
               float4 _AtmColor;
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

           bool rayIntersect(
               float3 O, // ray origin
               float3 D, // ray direction
               float3 C, // sphere center
               float R, // sphere radius
               out float A, // first intersection time
               out float B // second intersection time
           ) {
               float3 L = C - O;
               float DT = dot(L, D);
               float R2 = R*R;
               float CT2 = dot(L,L) - DT*DT;

               if (CT2 > R2) return false;

               float AT = sqrt(R2 - CT2);
               float TB = AT;
               A = DT - AT;
               B = DT + TB;
               return true;
           }

           bool lightSampling(float3 P, float3 L, out float opticalDepth) {
               float C1, C2;
               rayIntersect(P, L, float3(0,0,0), _PlanetRad+_AtmHeight, C1, C2);
               
               opticalDepth = 0;
               float time = 0;
               float3 C = P + L*C2;
               float dt = distance(P,C) / (float)_LightSamples;

               for (int i = 0; i < _LightSamples; i++) {
                   float3 Q = P + L * (time + dt*0.5);
                   float height = length(Q) - _PlanetRad;
                   if (height < 0) return false;
                   opticalDepth += exp(-height / _AtmScaleHeight) * dt;
                   time += dt;
               }

               return true;
           }

           float4 frag(Varyings IN) : SV_Target
           {
               // rayleigh scattering
               // the rayleigh scattering equation S takes the wavelength, scatter angle, and altitude of a scattering
               // event and determines the ratio of light scattered toward that particular direction
               // 
               //   S(wavelength, angle, altitude) = scatteringCoefficient(wavelength) * phase(angle) * density(altitude)
               // 
               // we know that scatteringCoefficient and phase will remain constant over the course of the view ray,
               // so we can just integrate density along the view ray and multiply the scattering coefficient
               // and phase at the very end.
               // 
               // however, note that to determine the out-scattering towards the view ray, we have to first
               // determine the intensity of light going in. light from the sun has to travel through the atmosphere
               // before it can scatter, so we'll have to integrate along the direction of the sun as well. this
               // is the purpose of lightSampling(), which 

               float3 L = normalize(_SunDir.xyz);
               float3 V = float3(0, 0, -1);

               float OSmag2 = IN.position.x*IN.position.x + IN.position.y*IN.position.y;

               float tA = 0, tB = 0; // times when camera ray enters and exits the atmosphere

               float rad2 = _PlanetRad * _PlanetRad;
               if (OSmag2 < rad2) {
                   // space -> sky -> planet
                   tB = -sqrt(rad2 - OSmag2);
                   rad2 = _PlanetRad + _AtmHeight; rad2 *= rad2;
                   tA = -sqrt(rad2 - OSmag2);
               } else {
                   rad2 = _PlanetRad + _AtmHeight; rad2 *= rad2;
                   if (OSmag2 < rad2) {
                       // space -> sky -> space
                       tA = -(tB = sqrt(rad2 - OSmag2));
                   } else {
                       return float4(0,0,0,1);
                   }
               }

               float opticalDepth = 0; // density
               float3 totalScattering = float3(0,0,0); // intensity * density
               float time = tA;
               float dt = (tB - tA) / (float)_ViewSamples;
               for (int i = 0; i < _ViewSamples; i++) {
                   float3 P = float3(IN.position.xy, time + dt * 0.5);
                   float height = length(P) - _PlanetRad;
                   float viewOpticalDepth = exp(-height / _AtmScaleHeight) * dt;
                   opticalDepth += viewOpticalDepth;

                   float lightOpticalDepth = 0;
                   bool overground = lightSampling(P, L, lightOpticalDepth);
                   if (overground) {
                       float3 attenuation = exp(-_RayleighScatteringCoeff.rgb * (opticalDepth + lightOpticalDepth));
                       totalScattering += viewOpticalDepth * attenuation;
                   }

                   time += dt;
               }

               float cosTheta = dot(V, L);
               float phase = 3.0 * (1.0 + cosTheta*cosTheta) / (16.0 * PI);

               float3 scattering = _SunIntensity * phase * _RayleighScatteringCoeff.rgb * totalScattering * _AtmSeaLevelPressure;
               float attenuation = exp(-_RayleighScatteringCoeff.w * opticalDepth);
               float4 c = _AtmColor;
               c.rgb *= scattering * c.w;
               c.a = attenuation;
               return c;
           }

           ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
