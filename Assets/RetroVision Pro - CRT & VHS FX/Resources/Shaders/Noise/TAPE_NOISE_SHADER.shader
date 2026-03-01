Shader "RetroVisionPro/CRT&VHSFX/TapeNoiseShader"
{
    Properties
    {
        _BlitTexture("CTexture", 2D) = "white" {} // source color buffer
    }

HLSLINCLUDE
    // Core URP math-helper library
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    // Blit.hlsl provides Vert, Attributes, Varyings, and _BlitTexture + sampler_LinearClamp
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

// Optional mask to confine effect -----------------------------------------------------
TEXTURE2D(_Mask);
SAMPLER(sampler_Mask);
#pragma shader_feature ALPHA_CHANNEL   // choose A or R channel at compile time
float MaskThreshold;

// Tunable parameters (resolved on CPU for performance) -------------------------------
half  _FadeMultiplier;     // 0..1  gate for mask usage
half  _TapeLinesAmount;    // 0..1  density of candidate lines per frame
half  _TapeNoiseFade;      // 0..+  brightness of noise contribution
half  _TapeNoiseAmount;    // 0..1  threshold for line activation (higher = fewer)
half  _TapeNoiseSpeed;     // Hz    animation rate
half  _NoiseLinesNum;      // virtual vertical resolution used for quantization
half  _NoiseQuantizeX;     // reserved; not used in core path
half  _DistAmount;         // pixel-scale horizontal displacement magnitude
int   _DistWidth;          // number of scan rows considered below a hit
int   _TailLength;         // length of trailing smear to the left
half  _TimeOverride;       // if 0, provide _Time.y from C# before dispatch
half  _PresetSeed;         // seed for light randomness per frame
half  M_TapeJitter;        // 0..N extra randomness multiplier
half  M_Fade;              // 0..1 global blend for this pass

// Loop caps keep shader unrolled and deterministic -----------------------------------
#define MAX_DIST 32
#define MAX_TAIL 24

// Simple screen blend in luma channel -------------------------------------------------
half Blend_Screen(half a, half b) { return 1.0h - (1.0h - a)*(1.0h - b); }

// YIQ transforms (separate luma from chroma) -----------------------------------------
half3 rgb2yiq(half3 c){
    return half3(
        0.2989h*c.x + 0.5959h*c.y + 0.2115h*c.z,   // Y
        0.5870h*c.x - 0.2744h*c.y - 0.5229h*c.z,   // I
        0.1140h*c.x - 0.3216h*c.y + 0.3114h*c.z    // Q
    );
}
half3 yiq2rgb(half3 c){
    return half3(
        c.x + c.y + c.z,
        0.956h*c.x - 0.2720h*c.y - 1.1060h*c.z,
        0.6210h*c.x - 0.6474h*c.y + 1.7046h*c.z
    );
}

// Hash/Noise utilities ----------------------------------------------------------------
float hash(float p){ p = frac(p * 0.1031); p *= p + 33.33; p *= p; return frac(p); }
half  hash1(half p){ p = frac(p * 0.1031h); p *= p + 33.33h; p *= p; return frac(p); }
half  hash12(half2 p){ p = frac(p * half2(443.897h, 441.423h)); p += dot(p, p + 19.19h); return frac(p.x * p.y); }
half  vnoise1(half x){ half i=floor(x), f=frac(x); half u=f*f*(3.0h-2.0h*f); return lerp(hash1(i), hash1(i+1.0h), u); }

// Row-wise noise signal along Y (models noisy tape scanlines) -------------------------
half TapeNoiseLines(half2 uv, half timeH)
{
    half pixelY = uv.y * (half)_ScreenParams.y;      // index rows in screen space
    // product of three slightly different 1D noises = richer distribution
    return vnoise1(pixelY*0.01h + timeH)
         * vnoise1(pixelY*0.011h + timeH)
         * vnoise1(pixelY*0.51h  + timeH);
}

// Converts the row noise into a binary “line hit” using a stochastic mod -------------
half TapeNoise(half lineLevel, half2 uv, half timeH, half TNA)
{
    // Per-pixel modulation to avoid uniform bars
    half nm = hash12(frac(uv + timeH * half2(0.234h, 0.637h)));
    nm = nm*nm*nm*nm + 0.3h;                         // compress then bias
    lineLevel *= nm;
    // Hit when lineLevel >= threshold (TNA). 1 or 0, no branches.
    return step(TNA, lineLevel);
}

half _TapeLineHueShift;      // base hue (radians)
half _TapeLineChromaGain;    // 0..1 chroma amplitude (≈0.6 authentic)
half _TapeLineRainbowScale;  // rainbow frequency along Y
half _TapeLineRainbowMode;   // 0 = simple hue, 1 = rainbow



half3 hsv2rgb(half3 h){
    half3 p = abs(frac(h.x + half3(0,2.0/3.0,1.0/3.0))*6.0h - 3.0h);
    return h.z * lerp(half3(1,1,1), saturate(p-1.0h), h.y);
}

half4 Frag(Varyings i) : SV_Target
{
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    // External time is expected; keep this pass deterministic if needed
    half t = _TimeOverride;

    // Build quantization factors once -------------------------------------------------
    half nLN   = max(_NoiseLinesNum, 1.0h);
    half aspect= (_ScreenParams.x/_ScreenParams.y);
    half SLN_X = 10.0h * ((half)_ScreenParams.x - nLN*aspect) + nLN*aspect; // legacy mapping kept
    half ONEXN = rcp(max(SLN_X, 1.0h));
    half ONEYN = rcp((half)_ScreenParams.y);

    // Working UVs: UV_FX is displaced/quantized; UV stays for the final sample -------
    half2 UV    = i.texcoord;
    half2 UV_FX = UV;
    UV_FX.x = floor(UV_FX.x * SLN_X) * ONEXN;

    half cfac = 0.0h; // суммарный вес линий для цветового тинта

    // Light per-frame jitter for speed and displacement -------------------------------
    float jitter = lerp(-0.1, 0.1, frac(_PresetSeed + hash(t)));
    _TapeNoiseSpeed *= (1.0 + jitter*0.05*M_TapeJitter);
    _DistAmount     *= (1.0 + jitter*0.05*M_TapeJitter);

    // Distortion scan across a vertical neighborhood ---------------------------------
    half cutMul   = saturate(_FadeMultiplier);       // single gate for masks
    half density  = saturate(_TapeLinesAmount);      // artistic “how often”
    half distThreshold = _TapeLinesAmount;           // threshold used against TapeNoiseLines()
    half distShift = 0.0h;                           // accumulates total shift strength

    // Clamp CPU-provided loop counts to shader caps ----------------------------------
    int W  = clamp(_DistWidth,  0, MAX_DIST);
    int TL = clamp(_TailLength, 0, MAX_TAIL);

    // Horizontal displacements from detected rows (branchless) ------------------------
    [unroll]
    for (int ii=0; ii<MAX_DIST; ii++)
    {
        // Sample row “ii” below current pixel in screen space
        half2 p   = half2(0.0h, UV_FX.y - ONEYN * (half)ii);
        half tnl  = TapeNoiseLines(p, t * _TapeNoiseSpeed);
        half tnl01= saturate(tnl);
        half hit  = step(distThreshold, tnl01) * step(ii, W-1);  // ignore iterations >= W

        // Smooth bell weight across the window width
        half sh = sin(PI * ((half)ii / max((half)W, 1.0h)));
        half dx = hit * sh * _DistAmount * cutMul * ONEXN;

        // Apply displacement to both FX and final UVs
        UV_FX.x -= dx;
        UV.x    -= dx;

        distShift += sh * hit;
    }

    // Fetch source once and move to YIQ for luma-only blending ------------------------
    half4 src   = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, UV);
    half3 signal= rgb2yiq(src.rgb);

    // Primary bright line at the distorted row ---------------------------------------
    half tnl0 = TapeNoiseLines(UV_FX, t * _TapeNoiseSpeed);
    half tn0  = TapeNoise(tnl0, UV_FX, t * _TapeNoiseSpeed, _TapeNoiseAmount);

    signal.x  = Blend_Screen(signal.x, tn0 * _TapeNoiseFade);    // luma screen

    // Leftward trail behind strong hits ----------------------------------------------
    [unroll]
    for (int j=0; j<MAX_TAIL; j++)
    {
        half ej = step(j, TL-1);                                   // stop beyond TL without branch
        half jj = (half)j;

        half2 d = half2(UV_FX.x - ONEXN * jj, UV_FX.y);

        half nl = TapeNoiseLines(d, t * _TapeNoiseSpeed);
        half tn = TapeNoise(nl, d, t * _TapeNoiseSpeed, _TapeNoiseAmount);

        // Randomize tail length per scanline
        half fadediff  = hash12(d + half2(0.01h,0.02h));
        half newlength = (half)TL * (1.0h - fadediff);

        // Linear fade along the tail, clamped by randomized max length
        half nsx  = ej * step(jj, max(newlength, 1e-6h)) * max(0.0h, 1.0h - jj / max(newlength, 1e-6h));
        half gate = step(0.8h, tn);                                 // only for strong hits

        signal.x = Blend_Screen(signal.x, nsx * _TapeNoiseFade * gate);
    }

    // Reduce chroma when displacement gets large (mimics head mis-tracking) ----------
    half scale = lerp(1.0h, rcp(max(distShift, 1e-4h)), step(0.4h, distShift));
    signal.yz *= scale;

        // Back to RGB --------------------------------------------------------------------
    half tapeMask = saturate(distShift);                 // tape-lines only
    half A1 = saturate(_TapeLineChromaGain);

    // simple hue (fixed across screen)
    half phiSimple = _TapeLineHueShift;
    half2 cSimple  = A1 * half2(cos(phiSimple), sin(phiSimple));

    // rainbow per scanline (+ slow time drift)
    half pixelY = UV_FX.y * (half)_ScreenParams.y;
    half phiRain = _TapeLineHueShift + 6.28318h * _TapeLineRainbowScale * (pixelY / max(nLN, 1.0h)) + (_TapeNoiseSpeed * t);
    half2 cRain = A1 * half2(cos(phiRain), sin(phiRain));

    // switch/blend: 0 = simple hue, 1 = rainbow
    half2 cTarget = lerp(cSimple, cRain, saturate(_TapeLineRainbowMode));

    // apply ONLY chroma where tape-lines exist
    signal.y = lerp(signal.y, cTarget.x, tapeMask);
    signal.z = lerp(signal.z, cTarget.y, tapeMask/5);

    half3 col = yiq2rgb(signal);

    // Optional mask, applied once at the end -----------------------------------------
    #if ALPHA_CHANNEL
        half alphaMask = step(0.0001h, SAMPLE_TEXTURE2D(_Mask, sampler_Mask, i.texcoord).a);
    #else
        half alphaMask = step(0.0001h, SAMPLE_TEXTURE2D(_Mask, sampler_Mask, i.texcoord).r);
    #endif
    half gate = lerp(1.0h, alphaMask, cutMul);

    // Final mix with global pass fade ------------------------------------------------
    return half4(lerp(src.rgb, col, M_Fade*gate), src.a);
}
ENDHLSL

SubShader
{
    Pass
    {
        Name "#NOISE#"
        Cull Off ZWrite Off ZTest Always               // standard full-screen post setup
        HLSLPROGRAM
        #pragma vertex   Vert
        #pragma fragment Frag
        ENDHLSL
    }
}
Fallback Off
}