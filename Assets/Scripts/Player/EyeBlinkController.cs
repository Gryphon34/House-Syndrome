using UnityEngine;
using UnityEngine.UI;

public class EyeBlinkController : MonoBehaviour
{
    public RectTransform topLid;
    public RectTransform bottomLid;

    [Header("Settings")]
    public float stamina = 100f;
    public float maxStamina = 100f;
    public float staminaDrainRate = 10f; // 눈 뜨고 있을 때 감소량 
    public float staminaRegenRate = 15f;  // 눈 감고 있을 때 회복량 
    public float scrollSensitivity = 0.1f;

    private float eyeOpenAmount = 1f; // 1: 다 뜬 상태, 0: 다 감은 상태 [cite: 18]
    private float topLidHeight;
    private float bottomLidHeight;

    void Start()
    {
        // 각 눈꺼풀의 높이를 가져와서 겹치지 않는 위치를 계산함
        if (topLid != null) topLidHeight = topLid.rect.height;
        if (bottomLid != null) bottomLidHeight = bottomLid.rect.height;
    }

    void Update()
    {
        HandleInput();
        HandleStamina();
        UpdateLidPositions();
    }

    void HandleInput()
    {
        float wheel = Input.GetAxis("Mouse ScrollWheel");
        if (stamina > 0) // 스태미나가 있을 때만 조절 가능 
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

        if (stamina <= 0)
        {
            eyeOpenAmount = Mathf.Lerp(eyeOpenAmount, 0f, Time.deltaTime * 5f);
        }
    }

    void UpdateLidPositions()
    {
        if (topLid == null || bottomLid == null) return;

        // 0(닫힘)일 때 Y=0, 1(열림)일 때 이미지 높이만큼 위/아래로 이동
        float topY = Mathf.Lerp(0, topLid.rect.height, eyeOpenAmount);
        float bottomY = Mathf.Lerp(0, -bottomLid.rect.height, eyeOpenAmount);

        topLid.anchoredPosition = new Vector2(0, topY);
        bottomLid.anchoredPosition = new Vector2(0, bottomY);
    }
}