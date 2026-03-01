using UnityEngine;

public class LanternLiquidGauge : MonoBehaviour
{
    [Header("Visual References")]
    public Transform liquidTransform;    // 랜턴 내부의 실린더(액체) 오브젝트
    public MeshRenderer liquidRenderer;  // 액체의 머티리얼 (Emission 조절용)
    public Light internalLight;          // 랜턴 내부의 포인트 라이트

    [Header("Height Settings")]
    public float minHeight = 0f;         // 게이지 0%일 때의 로컬 Y 스케일
    public float maxHeight = 1f;         // 게이지 100%일 때의 로컬 Y 스케일
    public float yOffset = 0.5f;         // 스케일이 커질 때 위로 올라가게 보이게 하는 위치 보정값

    [Header("Light Settings")]
    public float minIntensity = 0f;
    public float maxIntensity = 15f;
    [ColorUsage(true, true)]
    public Color energyColor = Color.white;

    private float maxTotalGauge;
    private Vector3 initialPosition;

    [Header("Glow Sensitivity")]
    [Range(1f, 5f)]
    public float glowCurve = 2.5f; // 수치가 높을수록 초반에 더 어둡고 후반에 급격히 밝아짐
    public float emissionMultiplier = 3f; // 기존 5f에서 조금 낮춤

    void Start()
    {
        if (liquidTransform != null) initialPosition = liquidTransform.localPosition;

        // HandInputSystem에서 최대 수치 가져오기
        HandInputSystem hand = FindFirstObjectByType<HandInputSystem>();
        if (hand != null)
        {
            maxTotalGauge = hand.maxGaugePerHand * 2f;
        }
    }

    void Update()
    {
        // 1. 현재 진행도 계산 (0.0 ~ 1.0)
        float currentTotal = HandInputSystem.leftGauge + HandInputSystem.rightGauge;
        float progress = Mathf.Clamp01(currentTotal / maxTotalGauge);

        // [핵심 수정] 진행도에 지수 곡선 적용 (초반 밝기 억제)
        float curvedProgress = Mathf.Pow(progress, glowCurve);

        // 2. 높이 조절 (높이는 정직하게 보여주는 것이 좋으므로 원래 progress 사용)
        if (liquidTransform != null)
        {
            float newHeight = Mathf.Lerp(minHeight, maxHeight, progress);
            liquidTransform.localScale = new Vector3(liquidTransform.localScale.x, newHeight, liquidTransform.localScale.z);
            liquidTransform.localPosition = initialPosition + new Vector3(0, newHeight * yOffset, 0);
        }

        // 3. 밝기 및 에미션 조절 (여기에는 곡선이 적용된 curvedProgress 사용)
        if (internalLight != null)
        {
            // 초반 10% 진행 시, 조명은 약 0.5%의 세기만 가짐 (2.5제곱 기준)
            internalLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, curvedProgress);
            internalLight.color = energyColor;
        }

        if (liquidRenderer != null)
        {
            // 에미션도 후반부에 터지도록 설정
            liquidRenderer.material.SetColor("_EmissionColor", energyColor * curvedProgress * emissionMultiplier);
        }
    }
}