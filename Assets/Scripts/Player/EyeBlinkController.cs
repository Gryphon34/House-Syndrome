using UnityEngine;
using UnityEngine.UI;

public class EyeBlinkController : MonoBehaviour
{
    public RectTransform topLid;
    public RectTransform bottomLid;

    [Header("Settings")]
    public float stamina = 100f; //
    public float maxStamina = 100f; //
    public float staminaDrainRate = 10f; //
    public float staminaRegenRate = 15f; //
    public float scrollSensitivity = 0.1f; //

    [Header("UI Elements")]
    public Slider staminaSlider; // 스태미너를 표시할 슬라이더
    public Image fillImage;      // 슬라이더의 채우기 이미지 (색상 변경용)
    
    [Header("UI Color Settings")]
    // [기본값 설정] 기본은 흰색, 낮을 때는 빨간색으로 인스펙터에서 설정되어 있다고 가정합니다.
    public Color highStaminaColor = Color.white;
    public Color midStaminaColor = Color.yellow;
    public Color lowStaminaColor = Color.red;

    [Header("Fatigue Visuals")]
    public float jitterThreshold = 30f; //
    public float jitterIntensity = 5f; //
    public float fatigueClosingSpeed = 0.5f; //

    [HideInInspector]
    public float eyeOpenAmount = 1f; //

    private float topLidHeight;
    private float bottomLidHeight;

    void Start()
    {
        if (topLid != null) topLidHeight = topLid.rect.height;
        if (bottomLid != null) bottomLidHeight = bottomLid.rect.height;

        // 슬라이더 초기화
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = stamina;
        }

        SetLidsActive(false);
    }

    void Update()
    {
        GameObject nightmarePlayer = GameObject.Find("NightMarePlayer");
        bool isNightmareActive = nightmarePlayer != null && nightmarePlayer.activeInHierarchy;

        if (!isNightmareActive)
        {
            if (topLid.gameObject.activeSelf) SetLidsActive(false);
            return;
        }

        if (!topLid.gameObject.activeSelf) SetLidsActive(true);

        HandleBlinkInput();
        HandleStamina();
        UpdateLidPositions();
        UpdateUI(); // UI 업데이트 함수 호출
    }

    void HandleBlinkInput()
    {
        float wheel = Input.GetAxis("Mouse ScrollWheel"); //
        eyeOpenAmount = Mathf.Clamp01(eyeOpenAmount + wheel * scrollSensitivity * 10f); //
    }

    void HandleStamina()
    {
        if (eyeOpenAmount > 0.1f) stamina -= staminaDrainRate * Time.deltaTime; //
        else stamina += staminaRegenRate * Time.deltaTime; //

        stamina = Mathf.Clamp(stamina, 0, maxStamina); //

        if (stamina < maxStamina * 0.5f && eyeOpenAmount > 0)
        {
            float fatigueWeight = 1f - (stamina / (maxStamina * 0.5f));
            eyeOpenAmount = Mathf.Lerp(eyeOpenAmount, 0f, Time.deltaTime * fatigueClosingSpeed * fatigueWeight);
        }

        if (stamina <= 0) eyeOpenAmount = Mathf.Lerp(eyeOpenAmount, 0f, Time.deltaTime * 5f);
    }

    // [수정된 함수] 스태미너 비율에 따라 색상을 선형 보간합니다.
    void UpdateUI()
    {
        if (staminaSlider != null)
        {
            staminaSlider.value = stamina;
        }

        if (fillImage != null)
        {
            float staminaPercent = stamina / maxStamina; // 0.0 ~ 1.0

            if (staminaPercent >= 0.7f)
            {
                // 구간 1: 70% ~ 100% (노란색에서 흰색으로 변화)
                // (staminaPercent - 0.7) / 0.3 을 통해 0.7~1.0 구간을 0~1 값으로 변환합니다.
                float t = (staminaPercent - 0.7f) / 0.3f;
                fillImage.color = Color.Lerp(midStaminaColor, highStaminaColor, t);
            }
            else if (staminaPercent >= 0.3f)
            {
                // 구간 2: 30% ~ 70% (빨간색에서 노란색으로 변화)
                // (staminaPercent - 0.3) / 0.4 를 통해 0.3~0.7 구간을 0~1 값으로 변환합니다.
                float t = (staminaPercent - 0.3f) / 0.4f;
                fillImage.color = Color.Lerp(lowStaminaColor, midStaminaColor, t);
            }
            else
            {
                // 구간 3: 0% ~ 30% (빨간색 유지 혹은 더 어두운 빨간색으로 변화)
                fillImage.color = lowStaminaColor;
            }
        }
    }

    void UpdateLidPositions()
    {
        if (topLid == null || bottomLid == null) return;

        float jitter = 0f;
        if (stamina < jitterThreshold && eyeOpenAmount > 0.1f) //
        {
            float fatigueScale = 1f - (stamina / jitterThreshold);
            jitter = Random.Range(-jitterIntensity, jitterIntensity) * fatigueScale;
        }

        float topY = Mathf.Lerp(0, topLidHeight, eyeOpenAmount) + jitter;
        float bottomY = Mathf.Lerp(0, -bottomLidHeight, eyeOpenAmount) - jitter;

        topLid.anchoredPosition = new Vector2(0, topY);
        bottomLid.anchoredPosition = new Vector2(0, bottomY);
    }

    void SetLidsActive(bool active)
    {
        if (topLid != null) topLid.gameObject.SetActive(active);
        if (bottomLid != null) bottomLid.gameObject.SetActive(active);
    }
}