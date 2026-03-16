/// Dot Crawl (Cross-Luma) 
Shader "RetroVisionPro/CRT&VHSFX/DotCrawl"
{
Properties { _BlitTexture("Source",2D)="white"{} }

HLSLINCLUDE
// Basic math and URP helpers
    // Core URP math-helper library
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    // Blit.hlsl provides Vert, Attributes, Varyings, and _BlitTexture + sampler_LinearClamp
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

TEXTURE2D(_Mask);      SAMPLER(sampler_Mask);

// Mask control
float _FadeMultiplier;
#pragma shader_feature ALPHA_CHANNEL
float MaskThreshold;

// Effect controls (set by the script/inspector)
float _Amount, _CombMix, _EdgeSensitivity, _CrawlSpeed, _EdgeMode;
float _DotOffsetPx, _DotFeatherPx, _DotSize;
float _BleedPower;
float _BleedBlurPx;

// Luma weights for Y
static const float3 LUMA=float3(0.299,0.587,0.114);

// RGB <-> YIQ conversion (Y = luma, I/Q = chroma)
float3 rgb2yiq(float3 c){
    return float3(dot(c,LUMA),
                  dot(c,float3( 0.596,-0.275,-0.321)),
                  dot(c,float3( 0.212,-0.523, 0.311)));
}
float3 yiq2rgb(float3 y){
    float Y=y.x,I=y.y,Q=y.z;
    return float3(Y+0.9563*I+0.6210*Q,
                  Y-0.2721*I-0.6474*Q,
                  Y-1.1070*I+1.7046*Q);
}

// Subcarrier phase along X. Rows flip phase to make a checker.
float carrier(float2 uv,float phase)
{
    float invX = rcp(_ScreenParams.x);
    float per  = (_ScreenParams.x/227.5) * _DotSize; // subcarrier period in pixels
    float xpx  = uv.x * _ScreenParams.x;
    float row  = floor(uv.y * _ScreenParams.y);
    float group= floor(row / max(1.0,_DotSize));     // group rows by DotSize
    return (TWO_PI * xpx / per) + (PI * fmod(group,2.0)) + phase;
}

// ---------------- edge detectors ----------------
// Each function returns edge strength in X and gives the sign of Y change.
float edgeCentralDiff(float2 uv,out float signY){
    float2 t=1.0/_ScreenParams.xy;
    float YL=dot(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv-float2(t.x,0)).rgb,LUMA);
    float YR=dot(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv+float2(t.x,0)).rgb,LUMA);
    float d=YR-YL; signY=(d>=0)?1.0:-1.0; return d;
}
float edgeSobelX(float2 uv,out float signY){
    float2 t=1.0/_ScreenParams.xy;
    float Ym1m1=dot(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv+float2(-t.x,-t.y)).rgb,LUMA);
    float Y0m1 =dot(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv+float2( 0,-t.y)).rgb,LUMA);
    float Yp1m1=dot(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv+float2( t.x,-t.y)).rgb,LUMA);
    float Ym10 =dot(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv+float2(-t.x, 0)).rgb,LUMA);
    float Yp10 =dot(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv+float2( t.x, 0)).rgb,LUMA);
    float Ym11 =dot(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv+float2(-t.x, t.y)).rgb,LUMA);
    float Y0p1 =dot(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv+float2( 0, t.y)).rgb,LUMA);
    float Yp11 =dot(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv+float2( t.x, t.y)).rgb,LUMA);
    float Gx=(Ym1m1-Yp1m1)+2.0*(Ym10-Yp10)+(Ym11-Yp11); signY=(Gx>=0)?1.0:-1.0; return Gx;
}
float edgePrewittX(float2 uv,out float signY){
    float2 t=1.0/_ScreenParams.xy;
    float Ym1m1=dot(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv+float2(-t.x,-t.y)).rgb,LUMA);
    float Y0m1 =dot(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv+float2( 0,-t.y)).rgb,LUMA);
    float Yp1m1=dot(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv+float2( t.x,-t.y)).rgb,LUMA);
    float Ym11 =dot(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv+float2(-t.x, t.y)).rgb,LUMA);
    float Y0p1 =dot(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv+float2( 0, t.y)).rgb,LUMA);
    float Yp11 =dot(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv+float2( t.x, t.y)).rgb,LUMA);
    float Gx=(Ym1m1+Y0m1+Yp1m1)-(Ym11+Y0p1+Yp11); signY=(Gx>=0)?1.0:-1.0; return Gx;
}
float edgeScharrX(float2 uv,out float signY){
    float2 t=1.0/_ScreenParams.xy;
    float Ym1m1=dot(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv+float2(-t.x,-t.y)).rgb,LUMA);
    float Yp1m1=dot(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv+float2( t.x,-t.y)).rgb,LUMA);
    float Ym10 =dot(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv+float2(-t.x, 0)).rgb,LUMA);
    float Yp10 =dot(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv+float2( t.x, 0)).rgb,LUMA);
    float Ym11 =dot(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv+float2(-t.x, t.y)).rgb,LUMA);
    float Yp11 =dot(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv+float2( t.x, t.y)).rgb,LUMA);
    float Gx=3*(Ym1m1-Yp1m1)+10*(Ym10-Yp10)+3*(Ym11-Yp11); signY=(Gx>=0)?1.0:-1.0; return Gx;
}
float edgeRoberts(float2 uv,out float signY){
    float2 t=1.0/_ScreenParams.xy;
    float Y00=dot(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv).rgb,LUMA);
    float Y10=dot(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv+float2(t.x,0)).rgb,LUMA);
    float Y01=dot(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv+float2(0,t.y)).rgb,LUMA);
    float Y11=dot(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv+float2(t.x,t.y)).rgb,LUMA);
    float Gx=Y00-Y11, Gy=Y10-Y01, g=abs(Gx)+abs(Gy); float d=Y10-Y00; signY=(d>=0)?1.0:-1.0; return g*signY;
}
float edgeChromaDiff(float2 uv,out float signY){
    float2 t=1.0/_ScreenParams.xy;
    float3 L=SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv-float2(t.x,0)).rgb;
    float3 R=SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv+float2(t.x,0)).rgb;
    float YL=dot(L,LUMA), YR=dot(R,LUMA); signY=(YR-YL)>=0?1.0:-1.0;
    // Use Cb/Cr delta for edges with same luma
    float2 CL=float2(dot(L,float3(-0.14713,-0.28886,0.436)),
                     dot(L,float3( 0.615  ,-0.515  ,-0.1)));
    float2 CR=float2(dot(R,float3(-0.14713,-0.28886,0.436)),
                     dot(R,float3( 0.615  ,-0.515  ,-0.1)));
    return length(CR-CL)*signY;
}

// Pick edge detector and build a soft mask [0..1]
float edgeMaskSelect(float2 uv,out float sideSign)
{
    float g,s; float m=_EdgeMode;
    if      (m<0.5) g=edgeCentralDiff(uv,s);
    else if (m<1.5) g=edgeSobelX(uv,s);
    else if (m<2.5) g=edgePrewittX(uv,s);
    else if (m<3.5) g=edgeScharrX(uv,s);
    else if (m<4.5) g=edgeRoberts(uv,s);
    else            g=edgeChromaDiff(uv,s);
    sideSign=s;
    // Threshold and gain depend on sensitivity
    float thr = lerp(0.25,0.02,saturate(_EdgeSensitivity));
    float gain= lerp(6.0 ,32.0,saturate(_EdgeSensitivity));
    return saturate((abs(g)-thr)*gain);
}

// -------- fast chroma bleed helpers --------
// Integrate edge mask from the edge to a target offset
#define EDGE_TAPS 8
float bleedFillOnce(float2 uv, float dir, float offU)
{
    float invT = 1.0 / (EDGE_TAPS-1.0);
    float acc=0, wsum=0, dummy;
    for (int k=0;k<EDGE_TAPS;k++)
    {
        float t = k * invT;                  // 0..1 along the ray
        float u = offU * t;
        float v = edgeMaskSelect(uv + float2(dir*u,0), dummy);
        float w = 1.0 - t;                   // triangle weight → stronger near edge
        acc += w * v; wsum += w;
    }
    return saturate(acc / max(wsum,1e-5));
}

// Blur and clamp to the correct side of the edge
float bleedFillFast(float2 uv, float dir, float offU, float blurPx, float sideSign)
{
    float base = bleedFillOnce(uv, dir, offU);

    // Enable blur only if > 0
    float blurOn = step(1e-4, blurPx);
    float dx = (blurPx * _DotSize) / _ScreenParams.x;
    float dy = dx * 0.6;

    float b_pdx = bleedFillOnce(uv + float2(+dx,0), dir, offU);
    float b_mdx = bleedFillOnce(uv + float2(-dx,0), dir, offU);
    float b_pdy = bleedFillOnce(uv + float2(0,+dy), dir, offU);
    float b_mdy = bleedFillOnce(uv + float2(0,-dy), dir, offU);

    // 5-tap smoothing
    float sm = (0.5*base + 0.2*(b_pdx+b_mdx) + 0.1*(b_pdy+b_mdy));
    float blended = lerp(base, sm, blurOn);

    // Keep only the correct side (dir vs edge sign)
    return blended * step(0.0, dir*sideSign);
}

half4 Frag (Varyings i) : SV_Target
{
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    float2 uv=i.texcoord; 
    float2 t=1.0/_ScreenParams.xy;

    // Read current and previous row
    float3 rgb0=SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
    float3 rgbU=SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv-float2(0,t.y)).rgb;

    // Convert to YIQ
    float3 yiq0=rgb2yiq(rgb0);
    float3 yiqU=rgb2yiq(rgbU);

    // Move the subcarrier over time
    float phase=_Time.y*max(0.0,_CrawlSpeed);
    float ph0=carrier(uv,phase);
    float phU=carrier(uv-float2(0,t.y),phase);

    // sincos is cheaper than separate sin and cos
    float s0,c0,sU,cU;
    sincos(ph0, s0, c0);
    sincos(phU, sU, cU);

    // Optional mask fade (multiplies _Amount)
    if (_FadeMultiplier > 0.0)
    {
    #if ALPHA_CHANNEL
        float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, i.texcoord).a;
    #else
        float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, i.texcoord).r;
    #endif
        float maskVal = smoothstep(MaskThreshold - 0.05, MaskThreshold + 0.05, m);
        _Amount *= maskVal;
    }

    // Build the encoded chroma components along the carrier
    float C0=yiq0.y*c0 + yiq0.z*s0;
    float CU=yiqU.y*cU + yiqU.z*sU;

    // Simple vertical comb (current minus previous row)
    float comp0 = yiq0.x+C0, compU=yiqU.x+CU;
    float Ccomb = 0.5*(comp0-compU);
    float a = Ccomb; // base strength near edges

    // Edge side (right/left) for correct dot orientation
    float sideSign; edgeMaskSelect(uv,sideSign);

    // Subcarrier quarter-period offset in UV
    float per=(_ScreenParams.x/227.5)*_DotSize;
    float baseOffU=(0.25*per)/_ScreenParams.x;
    float userOffU=(_DotOffsetPx*_DotSize)/_ScreenParams.x;
    float offU=baseOffU+userOffU;

    // Narrow masks right/left from the edge
    float dmy;
    float maskR=edgeMaskSelect(uv+float2(+userOffU,0),dmy)*step(0.0, sideSign);
    float maskL=edgeMaskSelect(uv+float2(-userOffU,0),dmy)*step(0.0,-sideSign);

    // Wide bleed from the edge to offU
    float stripR = bleedFillOnce(uv, +1.0, offU);
    float stripL = bleedFillOnce(uv, -1.0, offU);

    // Grid sine at both sides
    float sR0=sin(carrier(uv+float2(+offU,0),phase));
    float sL0=sin(carrier(uv+float2(-offU,0),phase));

    // Optional feather of the grid
    float featherOn = step(1e-4, _DotFeatherPx);
    float dxF=(_DotFeatherPx*_DotSize)/_ScreenParams.x;
    float sR1=0.5*sR0+0.25*sin(carrier(uv+float2(+offU+dxF,0),phase))+0.25*sin(carrier(uv+float2(+offU-dxF,0),phase));
    float sL1=0.5*sL0+0.25*sin(carrier(uv+float2(-offU+dxF,0),phase))+0.25*sin(carrier(uv+float2(-offU-dxF,0),phase));
    float sR=lerp(sR0,sR1,featherOn);
    float sL=lerp(sL0,sL1,featherOn);

    // Mix clean mask and grid by feather factor
    float k    = saturate(.7*(_DotFeatherPx));
    float mR   = lerp(1.0, sR, k);
    float mL   = lerp(1.0, sL, k);

    // Final dot term on Y (luma)
    float dotTerm = a * ( mR*maskR - mL*maskL ) * 5.0*_Amount;

    // Chroma bleed follows edge fill
    float fill = (stripR - stripL)*_Amount;
    float I = yiq0.y + _BleedPower * yiq0.y * fill;
    float Q = yiq0.z - _BleedPower * yiq0.z * fill;

    // Add dots to luma, then convert back to RGB
    float Y = yiq0.x + dotTerm;
    float3 outRGB = yiq2rgb(float3(Y,I,Q));
    return float4(outRGB,1);
}
ENDHLSL

SubShader
{
    Pass
    {
        Name "#DotCrawl#"
        Cull Off ZWrite Off ZTest Always
        HLSLPROGRAM
            #pragma fragment Frag
            #pragma vertex   Vert
        ENDHLSL
    }
}
Fallback Off
}
