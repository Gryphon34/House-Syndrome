Shader "RetroVisionPro/CRT&VHSFX/Distortion/VHS_STRETCH"
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
	float Fade;
	#pragma shader_feature ALPHA_CHANNEL
	float MaskThreshold;
    // Main texture and basic tunables
    float _Intensity;
	float screenLinesNum = 240.0;

	// Quantized sampling for noise look
	float noiseLinesNum = 240.0;
	float noiseQuantizeX = 1.0;

    // Feature toggle
	#pragma shader_feature VHS_STRETCH_ON

    // Stretch preset array:
    // (widthSteps, waveSpeed, lineFreqScale, lineFreqPhase)
	#define MAX_STRETCH_BANDS 6
	float4 _StretchBands[MAX_STRETCH_BANDS];

	// Runtime scratch (populated externally)
	float time_ = 0.0;
	float SLN = 0.0;
	float SLN_Noise = 0.0;
	float ONE_X = 0.0;
	float ONE_Y = 0.0;

    // Binary on/off helper (kept for compatibility)
	float onOff(float a, float b, float c, float t) 
    {
		return step(c, sin(t + a * cos(t * b)));
	}

	// RGB↔YIQ helpers (kept in case of color-domain processing)
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

    // Cosine stripe in 0..1 range along scanlines
	float Cos01Stripe(float2 uv, float stripeFreq, float phase)
	{
		return (cos(uv.y * PI * 2.0 * stripeFreq + phase) + 1.0) * 0.5;
	}

    // Core stretch operation for one band
    // widthSteps: number of discrete vertical bins
    // waveCycleSpeed: how fast the window cycles
    // lineFreqScale/Phase: line index modulation
	float2 ApplyStretch
	(
		float2 uv,
		float time,
		float widthSteps,        // was widthSteps
		float waveCycleSpeed,    // was wcs
		float lineFreqScale,     // was lfs
		float lineFreqPhase      // was lfp
	)
	{
		float tScaled   = time * waveCycleSpeed;
		float tWin      = tScaled - fmod(tScaled, 0.5);

		float width01 = Cos01Stripe(uv, 2.0 * (1.0 - frac(tWin)), PI - tWin)
		              * clamp(Cos01Stripe(uv, frac(tWin), tWin), 0.5, 1.0);

		width01 = floor(width01 * widthSteps);

		float lineIndex = (1.0 - frac(time * lineFreqScale + lineFreqPhase)) * screenLinesNum;
		lineIndex = lineIndex - frac(lineIndex);

		float invLines   = 1.0 / SLN;
		float denom      = max(width01, 1e-6);
		float modInWin   = fmod(lineIndex, denom);
		float shiftPhase = 1.0 - modInWin / denom;
		float scaleBins  = SLN / denom;

		float yLow  = invLines * (lineIndex - width01);
		float yHigh = invLines * lineIndex;

		float inBand = step(yLow  + 1e-6, uv.y) * step(uv.y, yHigh - 1e-6);

		float yNew = floor(uv.y * scaleBins + shiftPhase) / scaleBins - (shiftPhase - 1.0) / scaleBins;

		uv.y = lerp(uv.y, yNew, inBand);

		return uv;
	}

	// Linear repeat sampler declaration (not used directly here)
	SamplerState sampler_linear_repeat;

	// Fragment: applies up to MAX_STRETCH_BANDS and blends
	float4 Frag0(Varyings i) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
		float t = time_;
		float2 p = i.texcoord.xy;

		// Initialize line counts
		if (screenLinesNum == 0.0) screenLinesNum = _ScreenParams.y;
		SLN = screenLinesNum;
		SLN_Noise = noiseLinesNum;

		if (SLN_Noise == 0 || SLN_Noise > SLN) SLN_Noise = SLN;

		ONE_X = 1.0 / _ScreenParams.x;
		ONE_Y = 1.0 / _ScreenParams.y;

		// Apply each preset band in order; early-out when x <= 0
		[unroll]
		for (int it = 0; it < MAX_STRETCH_BANDS; ++it)
		{
			float4 pr = _StretchBands[it];
			if (pr.x <= 0.0) break; // x=widthSteps <= 0 → end of array
			p = ApplyStretch(p, t, pr.x, pr.y, pr.z, pr.w);
		}

		// Sampling and optional YIQ path retained
		half3 col = half3(0.0, 0.0, 0.0);
		half3 signal = half3(0.0, 0.0, 0.0);
		float2 pn = p;

		float ScreenLinesNumX = SLN_Noise * _ScreenParams.x / _ScreenParams.y;
		float SLN_X = noiseQuantizeX * (_ScreenParams.x - ScreenLinesNumX) + ScreenLinesNumX;
		pn.x = floor(pn.x * SLN_X) / SLN_X;

		float2 pn_ = pn * _ScreenParams.xy;

		float ONEXN = 1.0 / SLN_X;

		col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, p).rgb;
		signal = rgb2yiq(col);
		col = yiq2rgb(signal);

		half4 colIn = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);

                    // Mask
            if (_FadeMultiplier > 0.0)
            {
            #if ALPHA_CHANNEL
                float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, i.texcoord).a;
            #else
                float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, i.texcoord).r;
            #endif
                float maskVal = smoothstep(MaskThreshold - 0.05, MaskThreshold + 0.05, m);
                Fade *= maskVal;
            }

		// Final blend; user controls live outside this shader
		return lerp(colIn, half4(col, SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, p).a), Fade);
	}

    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "#VHS_STRETCH#"
			Cull Off ZWrite Off ZTest Always

            HLSLPROGRAM
                #pragma fragment Frag0
                #pragma vertex Vert
            ENDHLSL
        }
    }
    Fallback Off
}
