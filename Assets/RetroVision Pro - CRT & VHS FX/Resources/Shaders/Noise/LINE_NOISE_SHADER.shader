Shader "RetroVisionPro/CRT&VHSFX/LineNoiseShader"
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
	float _FadeMultiplier;
	#pragma shader_feature ALPHA_CHANNEL
	float MaskThreshold;

	half3 rgb2yiq(half3 c) 
	{
		return half3(
			(0.2989 * c.x + 0.5959 * c.y + 0.2115 * c.z),
			(0.5870 * c.x - 0.2744 * c.y - 0.5229 * c.z),
			(0.1140 * c.x - 0.3216 * c.y + 0.3114 * c.z)
			);
	};

	half3 yiq2rgb(half3 c) 
	{
		return half3(
			(1.0 * c.x + 1.0 * c.y + 1.0 * c.z),
			(0.956 * c.x - 0.2720 * c.y - 1.1060 * c.z),
			(0.6210 * c.x - 0.6474 * c.y + 1.7046 * c.z)
			);
	};
	float lineNoiseFade = 1.0;
	float lineNoiseAmount = 1.0;
	float lineNoiseSpeed = 5.0;

		float Hash2D01(float2 uv)
		{
			const float2 HASH_DOT   = float2(12.9898, 78.233);
			const float  HASH_SCALE = 43758.5453;

			float phase = fmod(dot(uv, HASH_DOT), 3.14);
			return frac(sin(phase) * HASH_SCALE);
		}

		float RandomLineSample(float2 uv, float time) 
		{
			float rnd = Hash2D01(float2(1.0, 2.0 * cos(time)) * time * 8.0 + uv);
			rnd *= rnd;
			return rnd;
		}
		float LineNoise(float2 uv, float time) 
		{
			float n = RandomLineSample(uv * float2(0.5, 1.0) + float2(1.0, 3.0), time) ;
			float freq = abs(sin(time));
			float phase = n * smoothstep(lineNoiseAmount, 1,fmod(uv.y * 4.0 + time / 2.0 + sin(time + sin(time * 0.63)), freq));
			return phase;
		}

	float4 Frag(Varyings i) : SV_Target
	{
		        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

		float t = _Time.y;
		float2 UV = i.texcoord.xy;

                    // Mask
            if (_FadeMultiplier > 0.0)
            {
            #if ALPHA_CHANNEL
                float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, i.texcoord).a;
            #else
                float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, i.texcoord).r;
            #endif
                float maskVal = smoothstep(MaskThreshold - 0.05, MaskThreshold + 0.05, m);
                lineNoiseFade *= maskVal;
            }

		half3 col = half3(0.0,0.0,0.0);
		half3 signal = half3(0.0,0.0,0.0);

		col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, UV).rgb;
		signal = rgb2yiq(col);

			signal.x += LineNoise(UV, t * lineNoiseSpeed)*lineNoiseFade ;

		col = yiq2rgb(signal);

		return half4(col, SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, UV).a);
	}

	ENDHLSL

	SubShader
	{
		Pass
		{
			Name "#LineNoiseShader#"
			Cull Off ZWrite Off ZTest Always

			HLSLPROGRAM
				#pragma fragment Frag
				#pragma vertex Vert
			ENDHLSL
		}

	}
	Fallback Off
}