using UnityEngine;

/// <summary>
/// 낮/밤 시간 진행과 조명만 담당. 날짜/스폰은 DayManager가 담당.
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    [Header("Day/Night Cycle Settings")]
    [Tooltip("하루를 0~1로 표현한 현재 시각. 0=자정, 0.25=아침, 0.5=정오, 0.75=저녁. 낮에만 증가하고 밤이 되면 멈춤.")]
    public float time;
    [Tooltip("낮이 흐르는 실제 시간(초). 예: 300이면 낮이 5분 동안 진행된 뒤 밤이 됨.")]
    public float fullDayLength;
    [Tooltip("게임 시작 시 time 초기값 (0~1). 예: 0.4 = 낮 중반쯤에서 시작.")]
    public float startTime = 0.4f;
    [Tooltip("0~1. 이 값 이상이면 '낮'으로 간주되고 시간이 흐름. (아침 시작 시각)")]
    public float dayStartTime = 0.25f;
    [Tooltip("0~1. 이 값에 도달하면 '밤'이 되고 시간이 멈춤. 잠자기 가능. (저녁/밤 시작 시각)")]
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
