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

    [Header("True Ending (amulet → BedPillow 순서 후 침대)")]
    [Tooltip("비우면 씬에서 이름이 spawn_point_true인 GameObject를 찾습니다.")]
    public Transform trueEndingSpawnPoint;

    [Header("Bad Ending (진엔딩 조건 미충족 시 잠들기)")]
    [Tooltip("비우면 씬에서 이름이 spawn_point_bad인 GameObject를 찾습니다.")]
    public Transform badEndingSpawnPoint;

    private bool isTransitioning = false;
    private bool _trueEndingBedUsed;
    private ItemInteraction _itemInteraction;

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

        _itemInteraction = FindObjectOfType<ItemInteraction>();
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
            // 베개(BedPillow)가 `Bed` 태그로 되어 있거나, 베개를 향해 레이가 베드를 가리켜도
            // 베개 아이템 상호작용이 우선되도록 침대 판정에서 제외합니다.
            Item hitItem = hit.transform.GetComponentInParent<Item>();
            if (hitItem != null && hitItem.itemName == "BedPillow")
            {
                if (sleepPromptUI != null) sleepPromptUI.SetActive(false);
                return;
            }

            if (hit.transform.CompareTag(bedTag))
            {
                bool trueEndingReady = !_trueEndingBedUsed
                    && _itemInteraction != null
                    && _itemInteraction.IsTrueEndingSpawnReady();
                bool ruleDone = SleepRuleManager.Instance != null && SleepRuleManager.Instance.CanSleep;

                if (trueEndingReady)
                {
                    if (sleepPromptUI != null) sleepPromptUI.SetActive(true);
                    if (Input.GetKeyDown(KeyCode.E))
                        StartCoroutine(TrueEndingSleepFadeAndTeleport());
                }
                else if (ruleDone)
                {
                    if (sleepPromptUI != null) sleepPromptUI.SetActive(true);
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        // 요구사항: Bad Ending(=spawn_point_bad)은 8일차에서만 적용
                        // 1~7일차는 원래대로 NightmareMap 이동(=NightMarePlayer 켜짐) 로직 사용.
                        bool isDay8 = SpawnManager.Instance != null && SpawnManager.Instance.GetCurrentDay() == 8;
                        bool hasAmulet = _itemInteraction != null && _itemInteraction.HasInteractedAmulet();
                        bool hasBedPillow = _itemInteraction != null && _itemInteraction.HasInteractedBedPillow();
                        bool noAmuletAndNoBedPillow = !hasAmulet && !hasBedPillow;

                        if (isDay8 && noAmuletAndNoBedPillow)
                            StartCoroutine(BadEndingSleepFadeAndTeleport());
                        else
                            StartCoroutine(SwapToNightmare());
                    }
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

        // 1. Fade Out
        float timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime;
            if (fadeImage != null) fadeImage.color = new Color(0, 0, 0, timer);
            yield return null;
        }

        // 2. Switch player states
        if (walkingPlayer != null) walkingPlayer.SetActive(false);
        if (nightmarePlayer != null) nightmarePlayer.SetActive(true);
        if (nightmareHUD != null) nightmareHUD.SetActive(true);

        // 작은 대기 후 Fade In
        yield return new WaitForSeconds(1f);

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

    Transform ResolveTrueEndingSpawn()
    {
        if (trueEndingSpawnPoint != null)
            return trueEndingSpawnPoint;
        GameObject go = GameObject.Find("spawn_point_true");
        return go != null ? go.transform : null;
    }

    Transform ResolveBadEndingSpawn()
    {
        if (badEndingSpawnPoint != null)
            return badEndingSpawnPoint;
        GameObject go = GameObject.Find("spawn_point_bad");
        return go != null ? go.transform : null;
    }

    static void TeleportTransformToSpawn(Transform playerRoot, Transform target)
    {
        if (playerRoot == null || target == null) return;
        CharacterController cc = playerRoot.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            playerRoot.position = target.position;
            playerRoot.rotation = target.rotation;
            cc.enabled = true;
        }
        else
        {
            playerRoot.position = target.position;
            playerRoot.rotation = target.rotation;
        }
    }

    IEnumerator TrueEndingSleepFadeAndTeleport()
    {
        yield return FadeOutTeleportFadeIn(
            ResolveTrueEndingSpawn(),
            "True ending: spawn_point_true 오브젝트를 찾을 수 없습니다. 씬에 이름 spawn_point_true를 두거나 BedInteraction.trueEndingSpawnPoint에 연결하세요.",
            () => { _trueEndingBedUsed = true; });
    }

    IEnumerator BadEndingSleepFadeAndTeleport()
    {
        yield return FadeOutTeleportFadeIn(
            ResolveBadEndingSpawn(),
            "Bad ending: spawn_point_bad 오브젝트를 찾을 수 없습니다. 씬에 이름 spawn_point_bad를 두거나 BedInteraction.badEndingSpawnPoint에 연결하세요.",
            null);
    }

    IEnumerator FadeOutTeleportFadeIn(Transform spawn, string missingSpawnLog, System.Action onTeleportOk)
    {
        isTransitioning = true;
        if (sleepPromptUI != null) sleepPromptUI.SetActive(false);

        float timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime;
            if (fadeImage != null) fadeImage.color = new Color(0, 0, 0, timer);
            yield return null;
        }

        if (spawn == null)
            Debug.LogError(missingSpawnLog);
        else if (walkingPlayer != null)
        {
            TeleportTransformToSpawn(walkingPlayer.transform, spawn);
            onTeleportOk?.Invoke();
        }

        yield return new WaitForSeconds(1f);

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            if (fadeImage != null) fadeImage.color = new Color(0, 0, 0, timer);
            yield return null;
        }

        isTransitioning = false;
    }
}