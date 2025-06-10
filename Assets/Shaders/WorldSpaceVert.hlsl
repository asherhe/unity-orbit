#ifndef WORLDSPACEVERT_INCLUDED
#define WORLDSPACEVERT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// vertex shaders that directly convert positions into world space

struct WSVAttributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct WSVVaryings
{
    float4 positionCS : SV_POSITION; // clip space position
    float4 position : TEXCOORD0; // world space position
    float2 uv : TEXCOORD1; // uv coords
};

// in world space centered around the object's position
WSVVaryings vert_centeredWS(WSVAttributes IN)
{
    WSVVaryings OUT;
    OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
    
    float3x3 o2wCentered = (float3x3) unity_ObjectToWorld;
    OUT.position = float4(mul(o2wCentered, (float3) IN.positionOS), 1);

    OUT.uv = IN.uv;

    return OUT;
}

#endif // WORLDSPACEVERT_INCLUDED
