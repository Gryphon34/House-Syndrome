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

    public float eyeOpenAmount = 1f;

    private float topLidHeight;
    private float bottomLidHeight;

    void Start()
    {
        if (topLid != null) topLidHeight = topLid.rect.height;
        if (bottomLid != null) bottomLidHeight = bottomLid.rect.height;

        // ½ÃÀÛÇÒ ¶§´Â ´«²¨Ç®À» ÀÏ´Ü ¼û±é´Ï´Ù.
        SetLidsActive(false);
    }

    // EyeBlinkController.csÀÇ Update ¹® È®ÀÎ

    void Update()
    {
        // NightMarePlayer°¡ ÄÑÁ® ÀÖÀ» ¶§¸¸ ´« ½Ã½ºÅÛ ÀÛµ¿
        GameObject nightmarePlayer = GameObject.Find("NightMarePlayer");
        bool isNightmareActive = nightmarePlayer != null && nightmarePlayer.activeInHierarchy;

        if (!isNightmareActive)
        {
            // ³·¿¡´Â ´«À» Ç×»ó ¶ß°í ÀÖ°Ô ÇÔ
            if (topLid.gameObject.activeSelf) SetLidsActive(false);
            eyeOpenAmount = 1f;
            stamina = maxStamina;
            return;
        }

        // ¹ãÀÌ¸é ´«²¨Ç® UI ÄÑ±â
        if (!topLid.gameObject.activeSelf) SetLidsActive(true);

        HandleInput();
        HandleStamina();
        UpdateLidPositions();
    }

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
        if (eyeOpenAmount > 0.1f) stamina -= staminaDrainRate * Time.deltaTime;
        else stamina += staminaRegenRate * Time.deltaTime;

        stamina = Mathf.Clamp(stamina, 0, maxStamina);

        if (stamina < maxStamina * 0.5f && eyeOpenAmount > 0)
        {
            float fatigueWeight = 1f - (stamina / (maxStamina * 0.5f));
            eyeOpenAmount = Mathf.Lerp(eyeOpenAmount, 0f, Time.deltaTime * fatigueClosingSpeed * fatigueWeight);
        }

        if (stamina <= 0) eyeOpenAmount = Mathf.Lerp(eyeOpenAmount, 0f, Time.deltaTime * 5f);
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

    // ´«²¨Ç®¸¸ ²°´Ù Ä×´Ù ÇÏ´Â ÇÔ¼ö
    void SetLidsActive(bool active)
    {
        if (topLid != null) topLid.gameObject.SetActive(active);
        if (bottomLid != null) bottomLid.gameObject.SetActive(active);
    }
}