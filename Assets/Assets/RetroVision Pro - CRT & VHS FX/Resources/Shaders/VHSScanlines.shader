Shader "RetroVisionPro/CRT&VHSFX/VHSScanlines"
{
    Properties
    {
        _BlitTexture("CTexture", 2D) = "white" {}
    }

    HLSLINCLUDE
    // Core URP math-helper library
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    // Blit.hlsl provides Vert, Attributes, Varyings, and _BlitTexture + sampler_LinearClamp
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
    TEXTURE2D(_Mask);      SAMPLER(sampler_Mask);
    // Mask fade
    float _FadeMultiplier;
    #pragma shader_feature ALPHA_CHANNEL
    float MaskThreshold;

    // ===== Parameters (renamed for clarity)
    float4 _ScanlineColor;      // color used on scanline bands
    float  _ScanlineDensity;    // density factor for band spacing
    float  _BandScrollSpeed;    // vertical/horizontal band scroll speed
    float  _BlendAmount;        // blend between source and scanline result (0..1)

    float  _JitterDivisor;      // divisor controlling wobble strength in distorted variants

    float  _FisheyeSpherical;   // fisheye spherical term
    float  _FisheyeBarrel;      // fisheye barrel term
    float  _FisheyeScale;       // fisheye scale

    // ===== Lens distortion (fisheye)
    float2 ApplyFisheyeDistortion(float2 uv, float spherical, float barrel, float scale)
    {
        float2 delta = uv - 0.5;
        float  r2    = dot(delta, delta);
        float  k     = 1.0 + r2 * (spherical + barrel * sqrt(r2));
        return k * scale * delta + 0.5;
    }

    // ===== Common band evaluation
    // Computes alternating scanline color based on a parameterized position scalar 'pScalar'
    float4 EvaluateScanlineBands(float4 srcRGBA, float pScalar, float lineSizeFactor)
    {
        // band size scales with screen height
        float lineSize     = _ScreenParams.y * lineSizeFactor;                 // 0.005 in original
        float bandSelector = (uint)(pScalar / floor(_ScanlineDensity * lineSize)) % 2;
        float4 bandColor   = (bandSelector == 0) ? srcRGBA : _ScanlineColor;
        return bandColor;
    }

// ===== Horizontal scanlines (no wobble)
float4 FragScanlinesHorizontal(Varyings i) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    float2 uvF = ApplyFisheyeDistortion(i.texcoord, _FisheyeSpherical, _FisheyeBarrel, _FisheyeScale);
    float4 src = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvF);

    // screen pixels (0..W/H)
    float2 ss01 = GetNormalizedScreenSpaceUV(i.positionCS);
    float  yPx  = ss01.y * _ScaledScreenParams.y;
    float  yPxCurved = uvF.y * _ScaledScreenParams.y;
    float  deltaYpx  = yPxCurved - yPx;              // bend from fisheye

    float displacement = fmod(_Time.x * 250.0 * _BandScrollSpeed, _ScaledScreenParams.y);
    float pScalar = displacement + yPx + deltaYpx;   // band position in pixels

    // band select in pixels
    float  lineSize = max(1.0, _ScaledScreenParams.y * 0.005);
    float  periodPx = max(1.0, floor(_ScanlineDensity * lineSize));
    uint   bandSel  = (uint)floor(pScalar / periodPx) & 1u;
    float4 bandColor= (bandSel == 0u) ? src : _ScanlineColor;

    float4 result = bandColor + src * i.texcoord.y;

    if (_FadeMultiplier > 0.0)
    {
    #if ALPHA_CHANNEL
        float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, i.texcoord).a;
    #else
        float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, i.texcoord).r;
    #endif
        float maskVal = smoothstep(MaskThreshold - 0.05, MaskThreshold + 0.05, m);
        _BlendAmount *= maskVal;
    }
    float4 baseCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);
    return lerp(baseCol, result, _BlendAmount);
}

// ===== Horizontal scanlines (distorted wobble)
float4 FragScanlinesHorizontalDistorted(Varyings i) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    float2 uvF = ApplyFisheyeDistortion(i.texcoord, _FisheyeSpherical, _FisheyeBarrel, _FisheyeScale);
    float4 src = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvF);

    float2 ss01 = GetNormalizedScreenSpaceUV(i.positionCS);
    float  yPx  = ss01.y * _ScaledScreenParams.y;
    float  yPxCurved = uvF.y * _ScaledScreenParams.y;
    float  deltaYpx  = yPxCurved - yPx;

    float displacement = fmod(_Time.x * 250.0 * _BandScrollSpeed, _ScaledScreenParams.y);

    // small wobble by x
    float yDist = frac(i.texcoord.y + cos((uvF.x + _Time.x * 0.25) * 100.0) / _JitterDivisor);
    float pScalar = displacement + yDist * _ScaledScreenParams.y + deltaYpx;

    float  lineSize = max(1.0, _ScaledScreenParams.y * 0.005);
    float  periodPx = max(1.0, floor(_ScanlineDensity * lineSize));
    uint   bandSel  = (uint)floor(pScalar / periodPx) & 1u;
    float4 bandColor= (bandSel == 0u) ? src : _ScanlineColor;

    float4 result = bandColor + src * yDist;

    if (_FadeMultiplier > 0.0)
    {
    #if ALPHA_CHANNEL
        float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, i.texcoord).a;
    #else
        float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, i.texcoord).r;
    #endif
        float maskVal = smoothstep(MaskThreshold - 0.05, MaskThreshold + 0.05, m);
        _BlendAmount *= maskVal;
    }
    float4 baseCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);
    return lerp(baseCol, result, _BlendAmount);
}

// ===== Vertical scanlines (no wobble)
float4 FragScanlinesVertical(Varyings i) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    float2 uvF = ApplyFisheyeDistortion(i.texcoord, _FisheyeSpherical, _FisheyeBarrel, _FisheyeScale);
    float4 src = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvF);

    float2 ss01 = GetNormalizedScreenSpaceUV(i.positionCS);
    float  xPx  = ss01.x * _ScaledScreenParams.x;
    float  xPxCurved = uvF.x * _ScaledScreenParams.x;
    float  deltaXpx  = xPxCurved - xPx;

    float displacement = fmod(_Time.x * 250.0 * _BandScrollSpeed, _ScaledScreenParams.y);
    float pScalar = displacement + xPx + deltaXpx;

    float  lineSize = max(1.0, _ScaledScreenParams.x * 0.005);
    float  periodPx = max(1.0, floor(_ScanlineDensity * lineSize));
    uint   bandSel  = (uint)floor(pScalar / periodPx) & 1u;
    float4 bandColor= (bandSel == 0u) ? src : _ScanlineColor;

    float4 result = bandColor + src * i.texcoord.y;

    if (_FadeMultiplier > 0.0)
    {
    #if ALPHA_CHANNEL
        float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, i.texcoord).a;
    #else
        float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, i.texcoord).r;
    #endif
        float maskVal = smoothstep(MaskThreshold - 0.05, MaskThreshold + 0.05, m);
        _BlendAmount *= maskVal;
    }
    float4 baseCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);
    return lerp(baseCol, result, _BlendAmount);
}

// ===== Vertical scanlines (distorted wobble)
float4 FragScanlinesVerticalDistorted(Varyings i) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    float2 uvF = ApplyFisheyeDistortion(i.texcoord, _FisheyeSpherical, _FisheyeBarrel, _FisheyeScale);
    float4 src = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvF);

    float2 ss01 = GetNormalizedScreenSpaceUV(i.positionCS);
    float  xPx  = ss01.x * _ScaledScreenParams.x;
    float  xPxCurved = uvF.x * _ScaledScreenParams.x;
    float  deltaXpx  = xPxCurved - xPx;

    float displacement = fmod(_Time.x * 250.0 * _BandScrollSpeed, _ScaledScreenParams.y);

    float xDist = frac(i.texcoord.x + cos((uvF.x + _Time.x * 0.25) * 100.0) / _JitterDivisor);
    float pScalar = displacement + xDist * _ScaledScreenParams.x + deltaXpx;

    float  lineSize = max(1.0, _ScaledScreenParams.x * 0.005);
    float  periodPx = max(1.0, floor(_ScanlineDensity * lineSize));
    uint   bandSel  = (uint)floor(pScalar / periodPx) & 1u;
    float4 bandColor= (bandSel == 0u) ? src : _ScanlineColor;

    float4 result = bandColor + src * i.texcoord.y;

    if (_FadeMultiplier > 0.0)
    {
    #if ALPHA_CHANNEL
        float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, i.texcoord).a;
    #else
        float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, i.texcoord).r;
    #endif
        float maskVal = smoothstep(MaskThreshold - 0.05, MaskThreshold + 0.05, m);
        _BlendAmount *= maskVal;
    }
    float4 baseCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);
    return lerp(baseCol, result, _BlendAmount);
}


    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment FragScanlinesHorizontal
            ENDHLSL
        }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment FragScanlinesHorizontalDistorted
            ENDHLSL
        }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment FragScanlinesVertical
            ENDHLSL
        }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment FragScanlinesVerticalDistorted
            ENDHLSL
        }
    }
    Fallback Off
}
