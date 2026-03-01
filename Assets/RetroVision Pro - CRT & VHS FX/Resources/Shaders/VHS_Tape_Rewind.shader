Shader "Hidden/Shader/VHS_Tape_Rewind"
{
    Properties
    {
        _BlitTexture("CTexture", 2D) = "white" {}   // source texture
    }

    HLSLINCLUDE
    // URP math helpers
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    // Fullscreen blit (gives Vert, Varyings, _BlitTexture, sampler_LinearClamp)
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    TEXTURE2D(_Mask);                // optional mask texture
    SAMPLER(sampler_Mask);

    // External fade by mask texture
    float _FadeMultiplier;
    #pragma shader_feature ALPHA_CHANNEL
    float MaskThreshold;

    // Base parameters
    half fade;                       // final blend 0..1
    half intencity;                  // effect power

    // Region controls
    half RegionHeight;               // 0..1. This is TOP edge position
    half RegionFeather;              // 0..1. Soft edge size

    // Animation controls
    half AnimMode;                   // 0..4
    half AnimSpeed;                  // 0..10
    half YShiftPx;                   // vertical shift in pixels

    // ---------- Helpers ----------
    // random helpers
    float Hash11(float x){ return frac(sin(x*78.233)*43758.5453); }
    float RandStep(float n){ return frac(sin(n*611.2)*43758.5453); }

    // Catmull-Rom spline
    float CR(float a,float b,float c,float d,float t){
        float t2=t*t, t3=t2*t;
        return 0.5*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t2+(-a+3*b-3*c+d)*t3);
    }

    // Smooth random hold
    float SmoothHold(float t, float rate){
        float x=t*max(rate,1e-3);
        float n=floor(x), u=frac(x);
        float p0=RandStep(n-1), p1=RandStep(n), p2=RandStep(n+1), p3=RandStep(n+2);
        return saturate(CR(p0,p1,p2,p3,u));
    }

    // Soft step between two values
    float SoftStepLerp(float a,float b,float u){
        float s = u*u*(3.0-2.0*u); // smoothstep
        return lerp(a,b,s);
    }

    // ---------- Envelope (AnimMode 0..4) ----------
    // Time envelope for effect power
    float Envelope(float t, float speed, float mode)
    {
        // 0: no animation
        if (mode < 0.5) return 1.0;

        speed = max(speed, 0.0001);

        if (mode < 1.5)
        {
            // 1: complex LFO
            float f1 = speed;
            float f2 = speed*0.41;
            float f3 = speed*1.73;
            float phaseWarp = 0.15*sin(6.2831853*speed*0.27*t);
            float a = 0.5 + 0.5*sin(6.2831853*f1*(t+phaseWarp));
            float b = 0.5 + 0.5*sin(6.2831853*f2*t + 6.2831853*RandStep(floor(t)));
            float c = 0.5 + 0.5*sin(6.2831853*f3*t);
            float micro = 0.02*sin(6.2831853*120.0*t)+0.015*sin(6.2831853*60.0*t);
            float e = a*0.55 + b*0.3 + c*0.15 + micro; // mix
            e = pow(saturate(e), 1.2);
            return lerp(0.35, 1.0, e);
        }
        else if (mode < 2.5)
        {
            // 2: stepped pulses
            float rate = speed*10.0;
            float h = SmoothHold(t, rate);
            float q = floor(h*5.0)/5.0;
            float e = SoftStepLerp(q, h, 0.35);
            return lerp(0.40, 1.0, e);
        }
        else if (mode < 3.5)
        {
            // 3: ramp and snap
            float segRate = speed*0.8;
            float seg = floor(t*segRate);
            float u = frac(t*segRate);
            float target = SmoothHold(t, segRate*0.7);
            float ramp   = smoothstep(0.0,1.0, pow(u,1.8));
            float trem   = 0.08*sin(6.2831853*(speed*1.3)*t);
            float e = saturate(0.75*ramp + 0.25*target + trem);
            float snap = smoothstep(0.98, 1.0, u);
            e = lerp(e, 0.45, snap);
            return lerp(0.35, 1.0, e);
        }
        else
        {
            // 4: burst mode
            float rate = max(speed * 1.8, 0.0001);

            float sum = 0.0;
            [unroll] for (int k = 0; k < 3; k++)
            {
                float phase = Hash11(100.0 * k) * 0.7;
                float tk = t + phase;
                float nk = floor(tk * rate);
                float uk = frac(tk * rate);

                float occur = Hash11(nk * 19.27 + k * 13.0);
                if (occur > 0.25)
                {
                    float start = Hash11(nk * 73.1 + k * 7.0) * 0.5;
                    float dur   = lerp(0.12, 0.65, Hash11(nk * 127.7 + k * 29.0));
                    float amp   = lerp(0.55, 1.00, Hash11(nk * 311.3 + k * 3.3));
                    float uRel  = (uk - start) / max(dur, 1e-3);

                    if (uRel > 0.0 && uRel < 1.0)
                    {
                        float attack = smoothstep(0.0, 0.18, uRel);
                        float decay  = exp(-max(uRel - 0.18, 0.0) * 4.0);
                        float body   = attack * decay * amp;
                        sum += body;
                    }
                }
            }

            float e = 1.0 - exp(-sum);                  // soft knee
            e += 0.06 * sin(6.2831853 * rate * 0.35 * t);
            return lerp(0.35, 1.0, saturate(e));
        }
    }

    // ---------- Fragment ----------
    half4 Frag(Varyings i) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

        // UV of full screen quad (0..1)
        float2 uv = i.texcoord;

        // time and envelope
        float t   = _Time.y;
        float env = Envelope(t, AnimSpeed, AnimMode);
        float da  = intencity * env;                  // dynamic power

        // ===== Region mask =====
        // Get 0..1 screen Y (works in XR)
        float y = GetNormalizedScreenSpaceUV(i.positionCS).y;
        // Pixel shift converted to 0..1
        float shift01 = YShiftPx / _ScaledScreenParams.y;
        float yS = saturate(y + shift01);

        // RegionHeight is TOP edge position
        float h = saturate(RegionHeight);
        float f = saturate(RegionFeather);

        // Soft top edge
        float mask = (1.0 - smoothstep(h - max(f,1e-5), h, yS)) * step(0.0, yS);

        // y for sampling inside region (0..1)
        float yEff = saturate(yS);

        // ===== Displacement sampling =====
        float2 displacementSampleUV = float2(uv.x + (_Time.y + 20) * 70, yEff);
        float displacement = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, displacementSampleUV).x * da * mask;

        // Polar to direction (no change of logic)
        float2 dir = float2(cos(displacement * 6.28318530718), sin(displacement * 6.28318530718));
        float2 displacedUV = uv + dir * displacement;

        // Shade and base color
        float4 shade = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, displacedUV);
        float4 main  = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

        // ===== Optional external mask =====
        fade = mask;                                  // start with region mask
        if (_FadeMultiplier > 0.0)
        {
        #if ALPHA_CHANNEL
            float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, uv).a;   // use alpha
        #else
            float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, uv).r;   // use red
        #endif
            float maskVal = smoothstep(MaskThreshold - 0.05, MaskThreshold + 0.05, m);
            fade *= maskVal;
        }

        // Final mix
        return lerp(main, shade, saturate(fade));
    }
    ENDHLSL

    Subshader
    {
        Pass
        {
            ZTest Always        // always draw
            Cull Off            // no culling
            ZWrite Off          // do not write depth
            HLSLPROGRAM
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
    Fallback off
}
