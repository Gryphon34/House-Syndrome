Shader "RetroVisionPro/CRT&VHSFX/Distortion/VHS_JITTER"
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

    // Optional mask to gate the effect
	TEXTURE2D(_Mask);
	SAMPLER(sampler_Mask);
	float _FadeMultiplier;
	#pragma shader_feature ALPHA_CHANNEL
    float MaskThreshold;
    // Main texture and base intensity (external control)
    float _Intensity;

    // Nominal scanline counts for shaping and quantization
	float screenLinesNum = 240.0;
	float noiseLinesNum  = 240.0;
	float noiseQuantizeX = 1.0;

    // Feature toggles (keywords)
	#pragma shader_feature VHS_JITTER_H_ON
	float jitterHAmount = 0.5;   // horizontal jitter magnitude

	#pragma shader_feature VHS_JITTER_V_ON
	float jitterVAmount = 1.0;   // vertical jitter magnitude (used in YIQ sampling)
	float jitterVSpeed  = 1.0;   // vertical jitter speed
	float jitterHSpeed  = 1.0;   // horizontal jitter speed

    // Runtime scratch (populated externally or derived here)
	float time_ = 0.0;
	float SLN = 0.0;
	float SLN_Noise = 0.0;
	float ONE_X = 0.0;
	float ONE_Y = 0.0;

    // Legacy on/off helper (kept for compatibility with other passes)
	float onOff(float a, float b, float c, float t) 
    {
		return step(c, sin(t + a * cos(t * b)));
	}

    // Hash-based pseudo-random generator from 2D coord
float RandomHash2D(float2 coord)
{
    const float dotX     = 12.9898;
    const float dotY     = 78.233;
    const float scale    = 43758.5453;
    const float piApprox = 3.14;

    float dotTerm = dot(coord, float2(dotX, dotY));
    float phase   = fmod(dotTerm, piApprox);
    float s       = sin(phase) * scale;
    return frac(s);
}

    // RGB <-> YIQ helpers (YIQ used to jitter chroma/luma independently)
	half3 rgb2yiq(half3 c) {
		return half3(
			(0.2989 * c.x + 0.5959 * c.y + 0.2115 * c.z),
			(0.5870 * c.x - 0.2744 * c.y - 0.5229 * c.z),
			(0.1140 * c.x - 0.3216 * c.y + 0.3114 * c.z)
			);
	};

	half3 yiq2rgb(half3 c) {
		return half3(
			(1.0 * c.x + 1.0 * c.y + 1.0 * c.z),
			(0.956 * c.x - 0.2720 * c.y - 1.1060 * c.z),
			(0.6210 * c.x - 0.6474 * c.y + 1.7046 * c.z)
			);
	};

    // Sample main texture in YIQ with per-channel horizontal jitter driven by noise and time
float3 SampleYIQWithVerticalJitter(float2 uv, float jitterAmount, float time)
{
    // Scale jitter into UV space (very small)
    jitterAmount *= 0.001;

    // Per-channel X offsets so Y/I/Q wobble differently
    float3 sampleX = float3(uv.x, uv.x, uv.x); 

    sampleX.r += (RandomHash2D(float2(time * 0.03,  uv.y * 0.42)) * 0.001 + sin( RandomHash2D(float2(time * 0.2, uv.y)) )) * jitterAmount;

    sampleX.g += (RandomHash2D(float2(time * 0.004, uv.y * 0.002)) * 0.004 + sin(time * 9.0)) * jitterAmount;

    // Fetch Y/I/Q independently along jittered X
    half3 yiq = half3(0.0, 0.0, 0.0);
    yiq.x = rgb2yiq(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, float2(sampleX.r, uv.y)).rgb).x; // Y
    yiq.y = rgb2yiq(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, float2(sampleX.g, uv.y)).rgb).y; // I
    yiq.z = rgb2yiq(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, float2(sampleX.b, uv.y)).rgb).z; // Q
    return yiq;
}

	// Declared for completeness; not explicitly used below
	SamplerState sampler_linear_repeat;

	// Main fragment: applies H jitter per-scanline mask and V jitter via YIQ sampling
	float4 Frag0(Varyings i) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
		float t = time_;
		float2 p = i.texcoord.xy;

        // Initialize scanline-related values
		if (screenLinesNum == 0.0) screenLinesNum = _ScreenParams.y;
		SLN = screenLinesNum;
		SLN_Noise = noiseLinesNum;
		if (SLN_Noise == 0 || SLN_Noise > SLN) SLN_Noise = SLN;

        // Pixel size in UV units
		ONE_X = 1.0 / _ScreenParams.x;
		ONE_Y = 1.0 / _ScreenParams.y;

        // --- Horizontal jitter mask: every other line gets a small phase shift
        // mask ∈ {0,1}; only masked lines receive the horizontal sine offset
		float mask = 1.0 - floor(frac(p.y * SLN * 0.5) * 2.0);
        p.x += mask * ONE_X * sin(t * 13000.0 * jitterHSpeed) * jitterHAmount;

        // Prepare YIQ sampling with vertical jitter
		half3 col    = half3(0.0, 0.0, 0.0);
		half3 signal = half3(0.0, 0.0, 0.0);
		float2 pn    = p;

        // Optional X quantization for a noisier, blocky feel
		float ScreenLinesNumX = SLN_Noise * _ScreenParams.x / _ScreenParams.y;
		float SLN_X = noiseQuantizeX * (_ScreenParams.x - ScreenLinesNumX) + ScreenLinesNumX;
		pn.x = floor(pn.x * SLN_X) / SLN_X;

		float2 pn_ = pn * _ScreenParams.xy; // prepared coords (not used further here)
		float ONEXN = 1.0 / SLN_X;

        // Sample with per-channel jitter in YIQ, then convert back to RGB
		signal = SampleYIQWithVerticalJitter(p, jitterVAmount, t * jitterVSpeed);
		col = yiq2rgb(signal);

        // Original color and mask-based fade
		half4 colIn = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);
		float fd = 1;

                    // Mask
            if (_FadeMultiplier > 0.0)
            {
            #if ALPHA_CHANNEL
                float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, i.texcoord).a;
            #else
                float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, i.texcoord).r;
            #endif
                float maskVal = smoothstep(MaskThreshold - 0.05, MaskThreshold + 0.05, m);
                fd *= maskVal;
            }

        // Final blend; magnitude driven outside by material/volume settings
		return lerp(colIn, half4(col, SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, p).a), fd);
	}


    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "#VHS_JITTER#"
			Cull Off ZWrite Off ZTest Always

            HLSLPROGRAM
                #pragma fragment Frag0
                #pragma vertex Vert
            ENDHLSL
        }
    }
    Fallback Off
}
