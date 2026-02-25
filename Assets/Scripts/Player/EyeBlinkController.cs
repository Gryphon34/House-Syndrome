using UnityEngine;
using UnityEngine.UI;

public class EyeBlinkController : MonoBehaviour
{
    public RectTransform topLid;
    public RectTransform bottomLid;

    [Header("Settings")]
    public float stamina = 100f;
    public float maxStamina = 100f;
    public float staminaDrainRate = 10f;
    public float staminaRegenRate = 15f;
    public float scrollSensitivity = 0.1f;

    [Header("Fatigue Visuals")]
    public float jitterThreshold = 30f;
    public float jitterIntensity = 5f;
    public float fatigueClosingSpeed = 0.5f;

    // [수정] 외부에서 접근 가능하도록 public으로 변경 (또는 Property 사용)
    public float eyeOpenAmount = 1f;

    private float topLidHeight;
    private float bottomLidHeight;

    void Start()
    {
        if (topLid != null) topLidHeight = topLid.rect.height;
        if (bottomLid != null) bottomLidHeight = bottomLid.rect.height;
    }

    void Update()
    {
        // [수정] 가위눌림 모드가 아닐 때(WalkingPlayer 상태 등)는 로직 중단
        // DayManager의 IsNightTime 상태를 확인하거나, 해당 UI가 켜져 있을 때만 작동하게 함
        if (SpawnManager.Instance != null && !SpawnManager.Instance.IsNightTime)
        {
            // 낮일 때는 눈을 항상 뜨고 있게 설정하고 리턴
            eyeOpenAmount = 1f;
            stamina = maxStamina;
            UpdateLidPositions();
            return;
        }

        HandleInput();
        HandleStamina();
        UpdateLidPositions();
    }

    // ... HandleInput, HandleStamina 기존 로직 동일

    void HandleInput()
    {
        float wheel = Input.GetAxis("Mouse ScrollWheel");
        if (stamina > 0)
        {
            eyeOpenAmount = Mathf.Clamp01(eyeOpenAmount + wheel * scrollSensitivity * 10f);
        }
    }

    void HandleStamina()
    {
        if (eyeOpenAmount > 0.1f)
        {
            stamina -= staminaDrainRate * Time.deltaTime;
        }
        else
        {
            stamina += staminaRegenRate * Time.deltaTime;
        }

        stamina = Mathf.Clamp(stamina, 0, maxStamina);

        if (stamina < maxStamina * 0.5f && eyeOpenAmount > 0)
        {
            float fatigueWeight = 1f - (stamina / (maxStamina * 0.5f));
            eyeOpenAmount = Mathf.Lerp(eyeOpenAmount, 0f, Time.deltaTime * fatigueClosingSpeed * fatigueWeight);
        }

        if (stamina <= 0)
        {
            eyeOpenAmount = Mathf.Lerp(eyeOpenAmount, 0f, Time.deltaTime * 5f);
        }
    }

    void UpdateLidPositions()
    {
        if (topLid == null || bottomLid == null) return;

        float jitter = 0f;
        if (stamina < jitterThreshold && eyeOpenAmount > 0.1f)
        {
            float fatigueScale = 1f - (stamina / jitterThreshold);
            jitter = Random.Range(-jitterIntensity, jitterIntensity) * fatigueScale;
        }

        float topY = Mathf.Lerp(0, topLidHeight, eyeOpenAmount) + jitter;
        float bottomY = Mathf.Lerp(0, -bottomLidHeight, eyeOpenAmount) - jitter;

        topLid.anchoredPosition = new Vector2(0, topY);
        bottomLid.anchoredPosition = new Vector2(0, bottomY);
    }
}