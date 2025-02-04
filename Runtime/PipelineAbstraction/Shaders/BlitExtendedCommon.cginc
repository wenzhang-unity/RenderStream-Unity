#ifndef BLIT_EXTENDED_COMMON_INCLUDED
#define BLIT_EXTENDED_COMMON_INCLUDED

// Based on Blit.hlsl from com.unity.render-pipelines.core

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

SamplerState sampler_LinearClamp;

TEXTURE2D(_BlitTexture);

uniform float4 _BlitScaleBias;
uniform float4 _BlitScaleBiasRt;

struct Attributes
{
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_Position;
    float2 texcoord   : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings Vert(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
    float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);

    output.positionCS = pos;
    output.texcoord   = uv * _BlitScaleBias.xy + _BlitScaleBias.zw;

    return output;
}

Varyings VertQuad(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float4 pos = GetQuadVertexPosition(input.vertexID);
    float2 uv  = GetQuadTexCoord(input.vertexID);

    output.positionCS    = pos * float4(_BlitScaleBiasRt.x, _BlitScaleBiasRt.y, 1, 1) + float4(_BlitScaleBiasRt.z, _BlitScaleBiasRt.w, 0, 0);
    output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
    output.texcoord      = uv * _BlitScaleBias.xy + _BlitScaleBias.zw;
    return output;
}

float4 FragBlit(Varyings input)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord.xy);
}

float4 FragNoConversion(Varyings input) : SV_Target
{
    return FragBlit(input);
}

float4 FragBlitLinearToSRGB(Varyings input) : SV_Target
{
    return LinearToSRGB(FragBlit(input));
}

float4 FragBlitSRGBToLinear(Varyings input) : SV_Target
{
    return SRGBToLinear(FragBlit(input));
}

#endif
