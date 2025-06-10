#ifndef SCATTERING_INCLUDED
#define SCATTERING_INCLUDED

// check for an intersection between a sphere and a ray
// return true if there exists an intersection
// if there is an intersection, A and B will also be filled with the enter and
// exit times of the ray through the sphere
bool rayIntersect(
    float3 O, // ray origin
    float3 D, // ray direction
    float3 C, // sphere center
    float R, // sphere radius
    out float A, // first intersection time
    out float B // second intersection time
)
{
    float3 L = C - O;
    float DT = dot(L, D);
    float R2 = R * R;
    float CT2 = dot(L, L) - DT * DT;

    if (CT2 > R2)
        return false;

    float AT = sqrt(R2 - CT2);
    float TB = AT;
    A = DT - AT;
    B = DT + TB;
    return true;
}

// get optical depth for rayleigh and mie scattering
// basically just integrating density across a ray of atmosphere
bool lightSampling(
    float3 P, // position
    float3 L, // sun direction
    float planetRadius,
    float atmHeight,
    float rayScaleHeight,
    float mieScaleHeight,
    int samples,
    bool throughGround, // whether to continue scattering through the ground
    out float rayOpticalDepth,
    out float mieOpticalDepth
)
{
    rayOpticalDepth = 0;
    mieOpticalDepth = 0;

    float C1, C2;
    if (!rayIntersect(P, L, float3(0, 0, 0), planetRadius + atmHeight, C1, C2))
        return true; // no atmosphere entry = didn't touch ground
    
    float time = 0;
    float3 C = P + L * C2;
    float dt = distance(P, C) / (float) samples;

    for (int i = 0; i < samples; i++)
    {
        float3 Q = P + L * (time + dt * 0.5);
        float height = length(Q) - planetRadius;
        if (height < 0 && !throughGround)
            return false;
        rayOpticalDepth += exp(-height / rayScaleHeight) * dt;
        mieOpticalDepth += exp(-height / mieScaleHeight) * dt;
        time += dt;
    }

    return true;
}

// attenuation factor for light passing through the atmosphere
float3 scatterAttenuation(
    float3 P, // position
    float3 L, // sun direction
    float planetRadius,
    float atmHeight,
    float rayScaleHeight,
    float mieScaleHeight,
    float4 rayCoeff,
    float4 mieCoeff,
    float miePhaseG,
    int samples,
    bool throughGround // whether to continue scattering through the ground
)
{
    float rayOpticalDepth;
    float mieOpticalDepth;
    bool overground = lightSampling(
        P, L,
        planetRadius,
        atmHeight,
        rayScaleHeight,
        mieScaleHeight,
        samples,
        throughGround,
        rayOpticalDepth,
        mieOpticalDepth
    );
    
    float rayPhase = 3.0 / (8.0 * PI);
    float miePhase = 3.0 * (1.0 - miePhaseG * miePhaseG) / (4.0 * PI * pow(1.0 + miePhaseG * miePhaseG - 2.0 * miePhaseG, 1.5) * (2.0 + miePhaseG * miePhaseG));
    return rayPhase * miePhase * exp(-(rayCoeff.rgb * rayOpticalDepth + mieCoeff.rgb * mieOpticalDepth));
}

// do rayleigh scattering on a 2d point
// output is unscaled, with sun intensity = 1, sea level pressure = 1, and no atmosphere tinting
// these effects can be applied later
float4 scatter2D(
    float2 pos, // 2d position of scatter point (world space centered at planet core)
    float3 L, // direction to light
    float planetRadius,
    float atmHeight,
    float rayScaleHeight,
    float mieScaleHeight,
    float4 rayCoeff,
    float4 mieCoeff,
    float miePhaseG,
    int2 samples // number of samples for x: out-scattering, y: in-scattering
)
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
    // is the purpose of lightSampling(), which samples the optical depth to the sun from a certain location in the atmosphere.

    float3 V = float3(0, 0, -1);

    float mag2 = pos.x * pos.x + pos.y * pos.y;

    float tA = 0, tB = 0; // times when camera ray enters and exits the atmosphere

    float rad2 = planetRadius * planetRadius;
    if (mag2 < rad2)
    {
        // space -> sky -> planet
        tB = -sqrt(rad2 - mag2);
        rad2 = planetRadius + atmHeight;
        rad2 *= rad2;
        tA = -sqrt(rad2 - mag2);
    }
    else
    {
        rad2 = planetRadius + atmHeight;
        rad2 *= rad2;
        if (mag2 < rad2)
        {
            // space -> sky -> space
            tA = -(tB = sqrt(rad2 - mag2));
        }
        else
        {
            return float4(0, 0, 0, 1);
        }
    }

    float rayOpticalDepth = 0; // density
    float mieOpticalDepth = 0;
    float3 rayTotalScattering = float3(0, 0, 0); // intensity * density
    float3 mieTotalScattering = float3(0, 0, 0);
    float time = tA;
    float dt = (tB - tA) / (float) samples.x;
    for (int i = 0; i < samples.x; i++)
    {
        float3 P = float3(pos, time + dt * 0.5);
        float height = length(P) - planetRadius;
        
        float rayViewOpticalDepth = exp(-height / rayScaleHeight) * dt;
        float mieViewOpticalDepth = exp(-height / mieScaleHeight) * dt;
        rayOpticalDepth += rayViewOpticalDepth;
        mieOpticalDepth += mieViewOpticalDepth;

        float rayLightOpticalDepth;
        float mieLightOpticalDepth;
        bool overground = lightSampling(
            P, L,
            planetRadius,
            atmHeight,
            rayScaleHeight,
            mieScaleHeight,
            samples.y,
            false,
            rayLightOpticalDepth,
            mieLightOpticalDepth
        );
        if (overground)
        {
            float3 attenuation = exp(-(rayCoeff.rgb * (rayOpticalDepth + rayLightOpticalDepth)
                                     + mieCoeff.rgb * (mieOpticalDepth + mieLightOpticalDepth)));
            rayTotalScattering += rayViewOpticalDepth * attenuation;
            mieTotalScattering += mieViewOpticalDepth * attenuation;
        }

        time += dt;
    }

    float cosTheta = dot(V, L);
    float rayPhase = 3.0 * (1.0 + cosTheta * cosTheta) / (16.0 * PI);
    float miePhase = (3.0 / (8.0 * PI)) * ((1.0 - miePhaseG * miePhaseG) * (1.0 + cosTheta * cosTheta))
                   / (pow(1.0 + miePhaseG * miePhaseG - 2.0 * miePhaseG * cosTheta, 1.5) * (2.0 + miePhaseG * miePhaseG));

    float3 rayScattering = rayPhase * rayCoeff.rgb * rayTotalScattering;
    float3 mieScattering = miePhase * mieCoeff.rgb * mieTotalScattering;
    float3 scattering = rayScattering + mieScattering;
    
    float attenuation = exp(-(rayCoeff.a * rayOpticalDepth + mieCoeff.a * mieOpticalDepth));
    
    return float4(scattering, attenuation);
}

#endif // SCATTERING_INCLUDED
