using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// ???? ????? ??: E?? ?? ?? ? ?? ??, ?? ? I?? ???? DayManager? ?? ??.
/// ?? ????? ??? DayManager? ??.
/// </summary>
public class BedInteraction : MonoBehaviour
{
    [Header("Player Objects")]
    public GameObject walkingPlayer;
    public GameObject nightmarePlayer;

    [Header("Cameras")]
    public Camera walkingCamera;    // ???? ???? ???? ?????????.
    public Camera nightmareCamera;  // ?????????? ???? ???? ?????????.

    [Header("UI & Effect")]
    public Image fadeImage;
    public GameObject sleepPromptUI;

    [Header("Settings")]
    public float interactDistance = 3f;
    public string bedTag = "Bed";

    private bool isTransitioning = false;

    public GameObject nightmareHUD; // ????? ???????? ?????? ??? ???????

    void Start()
    {
        // If nightmarePlayer is not assigned, try to find it by name
        if (nightmarePlayer == null)
        {
            GameObject found = GameObject.Find("NightMarePlayer");
            if (found != null) nightmarePlayer = found;
        }

        if (walkingPlayer != null) walkingPlayer.SetActive(true);
        if (nightmarePlayer != null) nightmarePlayer.SetActive(false);
        if (nightmareHUD != null) nightmareHUD.SetActive(false); // ?????? ?? HUD ??
        if (fadeImage != null) fadeImage.color = new Color(0, 0, 0, 0);
    }

    void Update()
    {
        if (isTransitioning) return;

        // ??(NightMare) ??: I? = ?? ?? ???, O? = 1??? ??
        if (nightmarePlayer != null && nightmarePlayer.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.I))
                StartCoroutine(WakeUpToNextDay());
            if (Input.GetKeyDown(KeyCode.O))
            {
                if (nightmareHUD != null) nightmareHUD.SetActive(false);
                nightmarePlayer.SetActive(false);
                if (walkingPlayer != null) walkingPlayer.SetActive(true);
                if (SpawnManager.Instance != null)
                    SpawnManager.Instance.ResetToDay1();
            }
            return;
        }

        CheckBed();
    }

    void CheckBed()
    {
        if (walkingCamera == null) return;

        // ?????? ???? ???? ???????? ???? ???
        Ray ray = walkingCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            if (hit.transform.CompareTag(bedTag))
            {
                bool isNight = SpawnManager.Instance != null && SpawnManager.Instance.IsNightTime;
                bool ruleDone = SleepRuleManager.Instance != null && SleepRuleManager.Instance.CanSleep;
                if (isNight && ruleDone)
                {
                    if (sleepPromptUI != null) sleepPromptUI.SetActive(true);
                    if (Input.GetKeyDown(KeyCode.E)) StartCoroutine(SwapToNightmare());
                }
                else
                {
                    if (sleepPromptUI != null) sleepPromptUI.SetActive(false);
                }
                return;
            }
        }
        if (sleepPromptUI != null) sleepPromptUI.SetActive(false);
    }

    IEnumerator SwapToNightmare()
    {
        isTransitioning = true;
        if (sleepPromptUI != null) sleepPromptUI.SetActive(false);

        // 1. ???? (Fade Out)
        float timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime;
            if (fadeImage != null) fadeImage.color = new Color(0, 0, 0, timer);
            yield return null;
        }

        walkingPlayer.SetActive(false);
        nightmarePlayer.SetActive(true);
        if (nightmareHUD != null) nightmareHUD.SetActive(true); // ???????? ???? ?? HUD ??

        // ?? ??? ??? ????
        yield return new WaitForSeconds(1f);

        // 3. ??? ????? (Fade In)
        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            if (fadeImage != null) fadeImage.color = new Color(0, 0, 0, timer);
            yield return null;
        }
        isTransitioning = false;
    }

    /// <summary>
    /// ?? ???? I? ?? ?: ?? ?? ??, ?? ???? ??, Walking ????? ??.
    /// DayManager? ?? ?? ??. DayManager.player?? Walking ???? Transform? ?????.
    /// </summary>
    IEnumerator WakeUpToNextDay()
    {
        isTransitioning = true;
        if (nightmareHUD != null) nightmareHUD.SetActive(false);

        float timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime;
            if (fadeImage != null) fadeImage.color = new Color(0, 0, 0, timer);
            yield return null;
        }

        if (SpawnManager.Instance != null)
            SpawnManager.Instance.AdvanceDayFromNightmare();

        if (nightmarePlayer != null) nightmarePlayer.SetActive(false);
        if (walkingPlayer != null) walkingPlayer.SetActive(true);

        yield return new WaitForSeconds(0.5f);

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            if (fadeImage != null) fadeImage.color = new Color(0, 0, 0, timer);
            yield return null;
        }

        isTransitioning = false;
    }
}