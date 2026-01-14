Shader "RetroVisionPro/CRT&VHSFX/AnalogFrameFeedbackURP"
{
    Properties { } // no material sliders here. All values come from C#.

    HLSLINCLUDE
    // URP core includes. Give math and fullscreen helpers.
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    // Feedback textures from last steps
    TEXTURE2D_X(_FeedbackTex);
    TEXTURE2D_X(_LastTex);
    SAMPLER(sampler_FeedbackTex);
    SAMPLER(sampler_LastTex);

    // Effect settings. Set from C#.
    float  feedbackThresh;    // cut off. small diff -> 0
    float  feedbackAmount;    // artefact strength
    float  feedbackFade;      // how fast old artefacts fade
    float4 feedbackColor;     // color tint for artefacts (rgb used)
    float  feedbackAmp;       // extra gain for mix pass
    float SpreadX = 1.0;


    // Screen blend. a over b. Common video style mix.
    half3 bm_screen(half3 a, half3 b) { return 1.0 - (1.0 - a) * (1.0 - b); }

    // === Pass 0 fragment: build artefact mask and color ===
    // Vert() and Varyings come from Blit.hlsl (full-screen triangle).
	float4 Frag(Varyings i) : SV_Target
	{
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

        // UV
		float2 p = i.texcoord.xy;

        // One pixel step in X. Use for neighbor sample.
		float one_x = _BlitTexture_TexelSize;
        float stepX = one_x * SpreadX;
        // Current frame color
		half3 fc = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord).rgb;

        // Last frame color (from history)
		half3 fl = SAMPLE_TEXTURE2D_X(_LastTex, sampler_LastTex, i.texcoord).rgb;

        // Simple motion/edge measure. Mean abs diff of RGB.
		float diff = abs(fl.x - fc.x + fl.y - fc.y + fl.z - fc.z) / 3.0;

        // Apply threshold. Small change -> zero.
		if (diff < feedbackThresh) diff = 0.0;

        // New artefact color from current frame * diff * amount
		half3 fbn = fc * diff * feedbackAmount;

        // Old feedback average (center and two X neighbors)
		half3 fbb = half3(0.0, 0.0, 0.0);

		fbb = (
				SAMPLE_TEXTURE2D_X(_FeedbackTex, sampler_FeedbackTex, i.texcoord).rgb +
				SAMPLE_TEXTURE2D_X(_FeedbackTex, sampler_FeedbackTex, i.texcoord + float2(stepX, 0.0)).rgb +
				SAMPLE_TEXTURE2D_X(_FeedbackTex, sampler_FeedbackTex, i.texcoord - float2(stepX, 0.0)).rgb
			  ) / 3.0;

        // Fade old artefacts
		fbb *= feedbackFade;

        // Screen blend: keep bright parts
		fbn = bm_screen(fbn, fbb);

        // Output color is artefact color with tint.
        // Alpha carries diff for later use.
		return half4(fbn * feedbackColor, SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord).a * diff);
	}
	
    // === Pass 1 fragment: composite artefacts with image ===
	float4 Frag1(Varyings i) : SV_Target
	{
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

        // Read source color
		float2 p = i.texcoord;
		half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);

        // Read artefact buffer
		half3 fbb = SAMPLE_TEXTURE2D_X(_FeedbackTex, sampler_FeedbackTex, i.texcoord).rgb;

        // Mix with screen blend and extra gain
		col.rgb = bm_screen(col.rgb, fbb );

        // Keep original alpha
		return col;
	}
    ENDHLSL

    SubShader
    {
        // URP setup. No depth write. Always draw. No cull.
        Tags{ "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        // Pass 0: build artefacts
        Pass
        {
            Name "AnalogFrameFeedbackBuild"
            HLSLPROGRAM
            #pragma vertex   Vert    // from Blit.hlsl
            #pragma fragment Frag    // build pass
            ENDHLSL
        }

        // Pass 1: composite artefacts with the image
        Pass
        {
            Name "AnalogFrameFeedbackComposite"
            HLSLPROGRAM
            #pragma vertex   Vert    // from Blit.hlsl
            #pragma fragment Frag1   // composite pass
            ENDHLSL
        }
    }
    Fallback Off
}