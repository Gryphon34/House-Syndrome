using UnityEngine;

/// <summary>
/// 낮/밤 시간 진행과 조명만 담당. 날짜/스폰은 DayManager가 담당.
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    [Header("Day/Night Cycle Settings")]
    public float time;
    public float fullDayLength;
    public float startTime = 0.4f;
    [Tooltip("0~1. 낮 시작 시각 (이때부터 낮)")]
    public float dayStartTime = 0.25f;
    [Tooltip("0~1. 밤 시작 시각 (이때부터 다음 날 시작 전까지 밤 유지)")]
    public float nightStartTime = 0.75f;
    private float timeRate;
    public Vector3 noon;
    [Header("Sun")]
    public Light sun;
    public Gradient sunColor;
    public AnimationCurve sunIntensity;
    [Header("Sun")]
    public Light moon;
    public Gradient moonColor;
    public AnimationCurve moonIntensity;
    [Header("Other Lighting")]
    public AnimationCurve lightingIntensiveMultiplier;
    public AnimationCurve reflectionIntensiveMultiplier;

    /// <summary> 현재가 밤 구간이면 true (DayManager 등에서 잠 가능 여부 판단용). </summary>
    public bool IsNightTime => time >= nightStartTime || time < dayStartTime;

    /// <summary> 다음 날 시작 시 DayManager에서 호출. 낮으로 리셋해 시간이 다시 흐르기 시작하게 함. </summary>
    public void ResetToDay()
    {
        time = dayStartTime;
    }

    private void Start()
    {
        timeRate = 1.0f / fullDayLength;
        time = startTime;
    }

    void UpdateLighting(Light lightSource, Gradient colorGradient, AnimationCurve intensiveCurve, float t)
    {
        float intensity = intensiveCurve.Evaluate(t);

        lightSource.transform.eulerAngles = (t - (lightSource == sun ? 0.25f : 0.75f)) * noon * 4.0f;
        lightSource.color = colorGradient.Evaluate(t);
        lightSource.intensity = intensity;

        GameObject go = lightSource.gameObject;
        if (lightSource.intensity == 0 && go.activeInHierarchy) go.SetActive(false);
        else if (lightSource.intensity > 0 && !go.activeInHierarchy) go.SetActive(true);
    }

    /// <summary>
    /// 현재 time이 밤 구간이면 밤이 끝날 때까지 쓸 고정 시간(0~1), 낮 구간이면 실제 time 반환.
    /// </summary>
    float GetEffectiveTimeForLighting()
    {
        bool isNight = time >= nightStartTime || time < dayStartTime;
        if (isNight)
        {
            // 밤 구간: 다음 날 시작 전까지 항상 같은 밤 상태 유지
            float nightFixed = nightStartTime + 0.12f;
            return nightFixed >= 1f ? nightFixed - 1f : nightFixed;
        }
        return time;
    }

    private void Update()
    {
        // 밤이 아닐 때만 시간 진행. 밤이 되면 그 상태로 멈춤 (사이클 없음)
        bool isNight = time >= nightStartTime || time < dayStartTime;
        if (!isNight)
        {
            time += timeRate * Time.deltaTime;
            if (time >= nightStartTime)
                time = nightStartTime;
        }

        float effectiveTime = GetEffectiveTimeForLighting();

        UpdateLighting(sun, sunColor, sunIntensity, effectiveTime);
        UpdateLighting(moon, moonColor, moonIntensity, effectiveTime);

        float ambientMultiplier = lightingIntensiveMultiplier.Evaluate(effectiveTime);
        RenderSettings.ambientLight = Color.white * ambientMultiplier;
        RenderSettings.reflectionIntensity = reflectionIntensiveMultiplier.Evaluate(effectiveTime);
    }
}
