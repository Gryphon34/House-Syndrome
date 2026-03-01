Shader "RetroVisionPro/CRT&VHSFX/TapeDistortionShader"
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

	TEXTURE2D(_Mask);
	SAMPLER(sampler_Mask);

	// External mask gate
	float _FadeMultiplier;
	#pragma shader_feature ALPHA_CHANNEL
	float MaskThreshold;
	// User controls
	float Fade;           // final blend 0..1
	half  waveAmount;     // base horizontal wobble strength
	half  tapeIntensity;  // distortion amplitude inside active tape band
	half  tapeSpeed;      // vertical travel speed of bands
	float TapeFreq;       // number of bands along Y
	float TapeWidth;      // band thickness (in normalized band phase)
	float TapeFeather;    // band edge softening

	// Hash in 0..1 from UV (tileable when fed with integer-grid coords)
	float hash(float2 uv)
	{
		const float2 HASH_DOT = float2(89.44, 19.36);
		const float  HASH_MUL = 22189.22;
		return frac(sin(dot(uv, HASH_DOT)) * HASH_MUL);
	}

	// Interpolated hash over a discrete grid (bilinear blend of cell hashes)
	float iHash(float2 uv, float2 gridRes)
	{
		float h00 = hash(floor(uv * gridRes + float2(0.0, 0.0)) / gridRes);
		float h10 = hash(floor(uv * gridRes + float2(1.0, 0.0)) / gridRes);
		float h01 = hash(floor(uv * gridRes + float2(0.0, 1.0)) / gridRes);
		float h11 = hash(floor(uv * gridRes + float2(1.0, 1.0)) / gridRes);

		float2 interp = smoothstep(float2(0.0, 0.0), float2(1.0, 1.0), fmod(uv * gridRes, 1.0));

		float hx0 = lerp(h00, h10, interp.x);
		float hx1 = lerp(h01, h11, interp.x);
		return lerp(hx0, hx1, interp.y);
	}

	// 3-octave FBM using iHash (low, mid, high frequency). Returns ~0..1
	float fbm3_iHash(float2 uv)
	{
		float s = 0.0;
		s += iHash(uv + float2(1,1),   float2(4,4))   * 0.5;
		s += iHash(uv + float2(2,2),   float2(8,8))   * 0.25;
		s += iHash(uv + float2(3,3), float2(16,16))   * 0.125;
		return s * (1.0 / (0.5 + 0.25 + 0.125)); // normalize weights
	}

	// Fragment: layered wobble + moving vertical bands with phase/noise modulation
	float4 Frag(Varyings i) : SV_Target
	{
		        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

		float2 uv  = i.texcoord;
		float2 uvn = uv; // warped UV

		// Base horizontal wobble from two FBM bands (slow + fast detail)
		uvn.x += (fbm3_iHash(float2(uvn.y,           _Time.y))         - 0.5) * 0.005 * waveAmount;
		uvn.x += (fbm3_iHash(float2(uvn.y * 100.0,   _Time.y * 10.0))  - 0.5) * 0.01  * waveAmount;

		// Compute vertical band phase p in 0..1, scrolling by tapeSpeed
		float p = frac(uvn.y * TapeFreq - _Time.y * tapeSpeed * 0.5);

		// Feathered rectangular window for the active band
		float feather = max(TapeFeather, fwidth(p)); // avoid undersampling
		float band = smoothstep(0.0, feather, p) * (1.0 - smoothstep(TapeWidth, TapeWidth + feather, p));

		// Phase magnitude inside the band, driven by FBM and tapeIntensity
		float tcPhase = band * fbm3_iHash(float2(_Time.y, _Time.y)) * tapeIntensity;

		// Additional high-frequency noise along Y that pushes horizontally
		float tcNoise = max(fbm3_iHash(float2(uvn.y * 100.0, _Time.y * 5.0)) - 0.5, 0.0);
		uvn.x = uvn.x - tcNoise * tcPhase; // horizontal tear-like shift

		// Source and warped samples
		float4 col1 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
		float4 col  = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvn);

		// Attenuate inside band and add a subtle luminance modulation
		col *= 1.0 - tcPhase;
		col *= 1.0 + clamp(fbm3_iHash(float2(0.0, uv.y + _Time.y * 0.2)) * 0.06 - 0.25, 0.0, 0.1);

                    // Mask
            if (_FadeMultiplier > 0.0)
            {
            #if ALPHA_CHANNEL
                float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, uv).a;
            #else
                float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, uv).r;
            #endif
                float maskVal = smoothstep(MaskThreshold - 0.05, MaskThreshold + 0.05, m);
                Fade *= maskVal;
            }

		// Final blend with original
		return lerp(col1, col, Fade);
	}

	ENDHLSL

	SubShader
	{
		Pass
		{
			Name "#TapeDistortionShader#"
			Cull Off ZWrite Off ZTest Always

			HLSLPROGRAM
				#pragma fragment Frag
				#pragma vertex Vert
			ENDHLSL
		}
	}
	Fallback Off
}
