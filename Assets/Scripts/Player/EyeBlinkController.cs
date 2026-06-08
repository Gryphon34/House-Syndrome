using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// NightMarePlayer에 부착합니다.
/// NightMarePlayer가 활성화되면 자동으로 동작하고,
/// 비활성화되면 Update()가 멈춰 자동으로 비활성 상태가 됩니다.
/// </summary>
public class EyeBlinkController : MonoBehaviour
{
    [Header("눈꺼풀 UI (NightMarePlayer 하위 Canvas의 RectTransform 할당)")]
    public RectTransform topLid;
    public RectTransform bottomLid;

    [Header("Settings")]
    public float stamina          = 100f;
    public float maxStamina       = 100f;
    [Tooltip("눈을 뜨고 있을 때 1초당 소모되는 스태미나")]
    public float staminaDrainRate = 10f;
    [Tooltip("눈을 감고 있을 때 1초당 회복되는 스태미나 (1초에 50%)")]
    public float staminaRegenRate = 50f;
    public float scrollSensitivity = 0.1f;

    [Header("UI Elements")]
    public Slider staminaSlider;
    public Image  fillImage;

    [Header("UI Color Settings (그라데이션)")]
    public Color highStaminaColor = new Color(1f,        1f,          1f);   // #FFFFFF (68~100%)
    public Color midStaminaColor  = new Color(1f, 235f/255f,    4f/255f);    // #FFEB04 (34~67%)
    public Color lowStaminaColor  = new Color(1f,        0f,          0f);   // #FF0000 (0~33%)

    [Header("Red Stamina Auto-Close (빨간색 구간)")]
    [Tooltip("이 비율(0~1) 이하로 스태미나가 떨어지면(빨간색 구간) TopLid/BottomLid가 자동으로 화면을 가립니다. 0.33 = 33%")]
    [Range(0f, 1f)]
    public float redStaminaThreshold = 0.33f;
    [Tooltip("빨간색 구간에서 눈이 자동으로 감긴 뒤, 다시 떠지기까지 걸리는 시간 (초)")]
    public float redStaminaEyeOpenDelay = 2f;

    [Header("Fatigue Visuals")]
    public float jitterThreshold = 30f;
    public float jitterIntensity = 5f;

    [HideInInspector]
    public float eyeOpenAmount = 1f;

    // ─── 내부 상태 ──────────────────────────────────────────────
    private float _topLidHeight;
    private float _bottomLidHeight;
    private bool  _lidHeightsReady = false;
    private float _eyeClosedTimer  = -1f; // ≥ 0: 0% 강제 감김 타이머 활성 중

    // 색상 그라데이션 밴드 경계 (사용자 정의: Low 0~33%, Mid 34~67%, High 68~100%)
    private const float LowBandTop = 0.33f; // 이 비율 이하 = 순수 빨강(#FF0000)
    private const float MidBandTop = 0.67f; // Low→Mid, Mid→High 그라데이션 경계
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// NightMarePlayer가 활성화될 때마다 호출 — 스태미나/눈 상태 초기화.
    /// </summary>
    void OnEnable()
    {
        stamina          = maxStamina;
        eyeOpenAmount    = 1f;
        _eyeClosedTimer  = -1f;
        _lidHeightsReady = false;

        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value    = maxStamina;
        }

        SetLidsActive(true);
    }

    /// <summary>
    /// NightMarePlayer가 비활성화될 때 호출 — 눈꺼풀 숨김.
    /// </summary>
    void OnDisable()
    {
        SetLidsActive(false);
    }

    void Update()
    {
        TryInitLidHeights();
        HandleBlinkInput();
        HandleStamina();
        UpdateLidPositions();
        UpdateUI();
    }

    // ──────────────────────────────────────────────────────────────
    // 눈꺼풀 높이 지연 초기화
    // Start()에서 읽으면 Canvas 레이아웃 미완료로 0이 반환되므로
    // Update에서 양수 값이 확인될 때 한 번만 저장합니다.
    // ──────────────────────────────────────────────────────────────
    void TryInitLidHeights()
    {
        if (_lidHeightsReady) return;

        float th = topLid    != null ? topLid.rect.height    : 0f;
        float bh = bottomLid != null ? bottomLid.rect.height : 0f;

        if (th > 0f && bh > 0f)
        {
            _topLidHeight    = th;
            _bottomLidHeight = bh;
            _lidHeightsReady = true;
            Debug.Log($"[EyeBlinkController] 눈꺼풀 높이 초기화 완료 — top:{_topLidHeight:F1}, bottom:{_bottomLidHeight:F1}");
        }
    }

    // ──────────────────────────────────────────────────────────────
    // 스크롤 휠 → eyeOpenAmount 조절
    // 빨간색 자동 감김 타이머 중에만 스크롤 비활성화 (그 외에는 항상 자유)
    // 플레이어는 눈을 감아 스태미나를 회복할 수 있습니다.
    // ──────────────────────────────────────────────────────────────
    void HandleBlinkInput()
    {
        if (_eyeClosedTimer >= 0f) return; // 빨간색 자동 감김 중에는 입력 무시

        float wheel = Input.GetAxis("Mouse ScrollWheel");
        eyeOpenAmount = Mathf.Clamp01(eyeOpenAmount + wheel * scrollSensitivity * 10f);
    }

    // ──────────────────────────────────────────────────────────────
    // 스태미나 소모·회복 및 빨간색 구간 자동 감김
    //
    //  눈 뜸 (eyeOpenAmount > 0.1)  → 스태미나 소모 (staminaDrainRate)
    //  눈 감음                      → 스태미나 회복 (staminaRegenRate, 1초에 50%)
    //  빨간색 구간 (≤ redStaminaThreshold)
    //      → TopLid/BottomLid가 자동으로 화면을 가림(eyeOpenAmount = 0)
    //      → redStaminaEyeOpenDelay(2초) 후 자동으로 눈 뜸
    // ──────────────────────────────────────────────────────────────
    void HandleStamina()
    {
        // ─── 빨간색 자동 감김 대기 타이머 처리 ──────────────────────────
        if (_eyeClosedTimer >= 0f)
        {
            eyeOpenAmount = 0f;                              // 눈 완전히 감힘(화면 가림) 유지
            stamina += staminaRegenRate * Time.deltaTime;   // 감고 있으므로 회복
            stamina  = Mathf.Clamp(stamina, 0f, maxStamina);

            _eyeClosedTimer -= Time.deltaTime;
            if (_eyeClosedTimer <= 0f)
            {
                _eyeClosedTimer = -1f;
                eyeOpenAmount   = 1f;                        // 타이머 완료 → 자동으로 눈 뜸
            }
            return;
        }

        // ─── 일반 소모/회복 ─────────────────────────────────────────────
        if (eyeOpenAmount > 0.1f)
            stamina -= staminaDrainRate * Time.deltaTime;   // 눈 뜸 → 소모
        else
            stamina += staminaRegenRate * Time.deltaTime;   // 눈 감음 → 회복 (1초에 50%)

        stamina = Mathf.Clamp(stamina, 0f, maxStamina);

        // ─── 빨간색 구간 진입 → 강제 감김 + 2초 타이머 시작 ─────────────
        if (stamina / maxStamina <= redStaminaThreshold)
        {
            eyeOpenAmount   = 0f;                            // TopLid/BottomLid가 화면을 가림
            _eyeClosedTimer = redStaminaEyeOpenDelay;
        }
    }

    // ──────────────────────────────────────────────────────────────
    // 눈꺼풀 위치 갱신
    // ──────────────────────────────────────────────────────────────
    void UpdateLidPositions()
    {
        if (topLid == null || bottomLid == null) return;
        if (!_lidHeightsReady) return;

        float jitter = 0f;
        if (stamina < jitterThreshold && eyeOpenAmount > 0.1f)
        {
            float fatigueScale = 1f - (stamina / jitterThreshold);
            jitter = Random.Range(-jitterIntensity, jitterIntensity) * fatigueScale;
        }

        float topY    = Mathf.Lerp(0f,  _topLidHeight,    eyeOpenAmount) + jitter;
        float bottomY = Mathf.Lerp(0f, -_bottomLidHeight, eyeOpenAmount) - jitter;

        topLid.anchoredPosition    = new Vector2(0f, topY);
        bottomLid.anchoredPosition = new Vector2(0f, bottomY);
    }

    // ──────────────────────────────────────────────────────────────
    // 스태미나 UI 갱신 — Low/Mid/High 색상을 끊김 없이 그라데이션 처리
    //
    //   0% ──────── 33% ──────── 67% ──────── 100%
    //   #FF0000(Low) │  ───► #FFEB04(Mid) ───► #FFFFFF(High)
    //   빨강(고정)   └──── 노랑 ────────────► 흰색
    //
    //   Low 구간(0~33%)은 순수 빨강으로 고정 → 빨간색 구간이 확실히 보입니다.
    //   33% 이상부터 노랑·흰색으로 부드럽게 그라데이션됩니다.
    // ──────────────────────────────────────────────────────────────
    void UpdateUI()
    {
        if (staminaSlider != null)
            staminaSlider.value = stamina;

        if (fillImage != null)
        {
            float pct = stamina / maxStamina;
            Color c;

            if (pct <= LowBandTop)
            {
                c = lowStaminaColor;                         // 0~33% : 빨강
            }
            else if (pct <= MidBandTop)
            {
                float t = (pct - LowBandTop) / (MidBandTop - LowBandTop);
                c = Color.Lerp(lowStaminaColor, midStaminaColor, t);   // 33~67% : 빨강→노랑
            }
            else
            {
                float t = (pct - MidBandTop) / (1f - MidBandTop);
                c = Color.Lerp(midStaminaColor, highStaminaColor, t);  // 67~100% : 노랑→흰색
            }

            fillImage.color = c;
        }
    }

    void SetLidsActive(bool active)
    {
        if (topLid    != null) topLid.gameObject.SetActive(active);
        if (bottomLid != null) bottomLid.gameObject.SetActive(active);
    }
}
