using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ItemInteraction : MonoBehaviour
{
    public float interactDistance = 3f;
    [Tooltip("WalkingPlayer ?? ???. ?? ??? 'WalkingPlayer' ???? ??")]
    public Camera walkingCamera;
    public GameObject interactPromptUI; // "??????? (E)" ????
    public TextMeshProUGUI logText;     // ?????? ??? ?? ????? ????

    [Header("Inventory")]
    public List<string> collectedItems = new List<string>();

    [Header("Newspaper UI")]
    [Tooltip("E키로 newspaper 아이템과 상호작용했을 때 화면 전체에 띄울 신문 UI 루트 오브젝트 (Canvas 하위 Panel 등)")]
    public GameObject newspaperUIRoot;

    [Header("Clue Viewer UI")]
    [Tooltip("clue_1 등 클루 아이템 E키 상호작용 시 화면 전체에 띄울 UI 루트 (Canvas 하위 Panel 등)")]
    public GameObject clueViewerUIRoot;
    [Tooltip("클루 이미지를 표시할 Image. clueViewerUIRoot 하위에 두거나 동일 패널에 붙이면 됨.")]
    public Image clueViewerImage;
    [Tooltip("아이템 이름별로 표시할 스프라이트. 예: clue_1 → clue_1 스프라이트")]
    public List<ClueImageEntry> clueImages = new List<ClueImageEntry>();

    [System.Serializable]
    public class ClueImageEntry
    {
        public string itemName;
        public Sprite sprite;
    }

    [Header("Voice Recorder UI")]
    [Tooltip("voice_recorder 등 E키 상호작용 시 화면에 띄울 UI 루트 (Clue Viewer와 동일하게 X로 닫기)")]
    public GameObject voiceRecorderUIRoot;
    [Tooltip("녹음기 UI에 표시할 Image. voiceRecorderUIRoot 하위에 두면 됨.")]
    public Image voiceRecorderImage;
    [Tooltip("아이템 이름별 스프라이트. 예: voice_recorder → UI용 스프라이트")]
    public List<VoiceRecorderImageEntry> voiceRecorderImages = new List<VoiceRecorderImageEntry>();

    [System.Serializable]
    public class VoiceRecorderImageEntry
    {
        public string itemName;
        public Sprite sprite;
        [Tooltip("사진을 클릭했을 때 재생할 소리 (VoiceRecorderPhotoClick 컴포넌트가 Image에 있어야 함)")]
        public AudioClip clickSound;
    }

    [Tooltip("녹음기 UI에서 클릭 사운드 재생용. 비우면 PlayClipAtPoint로 재생합니다.")]
    public AudioSource voiceRecorderAudioSource;

    [Header("Diary UI")]
    [Tooltip("diary 아이템 E키 시 화면 전체에 띄울 일기 UI 루트 (Canvas 하위 Panel 등)")]
    public GameObject diaryUIRoot;
    [Tooltip("일기 페이지 이미지를 표시할 Image. 순서대로 diaryPages[0]=첫 페이지(일기_1), [1]=두 번째(일기_2)")]
    public Image diaryImage;
    [Tooltip("일기 페이지 스프라이트 순서. [0]=일기_1, [1]=일기_2 …")]
    public List<Sprite> diaryPages = new List<Sprite>();
    [Tooltip("오른쪽으로 넘기라는 안내 문구를 표시할 Text. 비워두면 표시 안 함.")]
    public TextMeshProUGUI diaryPageHintText;

    bool _isNewspaperOpen = false;
    bool _isClueViewerOpen = false;
    bool _isVoiceRecorderOpen = false;
    bool _isDiaryOpen = false;

    /// <summary>PlayerController 등에서 UI 열림 중 커서 잠금/카메라 회전을 막기 위해 사용합니다.</summary>
    public static bool IsVoiceRecorderUiOpen { get; private set; }

    AudioClip _voiceRecorderClickClip;
    int _diaryCurrentPage = 0;

    /// <summary> amulet 수집 후 BedPillow를 E키로 상호작용했을 때만 true (순서 강제). 오브젝트는 유지. </summary>
    bool _bedPillowTrueEndingDone;

    /// <summary>
    /// BedPillow를 E로 한 번이라도 눌렀는지 여부(아뮬릿 유무와 무관).
    /// Bad Ending 조건에서 "아무 아이템도 안 건드렸는지" 판별에 사용.
    /// </summary>
    bool _bedPillowInteracted;

    void Update()
    {
        if (_isNewspaperOpen)
        {
            if (Input.GetKeyDown(KeyCode.X))
                CloseNewspaper();
            return;
        }
        if (_isClueViewerOpen)
        {
            if (Input.GetKeyDown(KeyCode.X))
                CloseClueViewer();
            return;
        }
        if (_isVoiceRecorderOpen)
        {
            if (Input.GetKeyDown(KeyCode.X))
                CloseVoiceRecorder();
            return;
        }
        if (_isDiaryOpen)
        {
            if (Input.GetKeyDown(KeyCode.X))
                CloseDiary();
            else if (Input.GetKeyDown(KeyCode.RightArrow))
                DiaryNextPage();
            return;
        }

        // ???????? ????? ??? ?? ??
        if (DifficultyManager.Instance == null || walkingCamera == null || !walkingCamera.gameObject.activeInHierarchy)
        {
            interactPromptUI.SetActive(false);
            return;
        }

        CheckItem();
    }

    void CheckItem()
    {
        Ray ray = walkingCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, interactDistance))
        {
            interactPromptUI.SetActive(false);
            return;
        }

        // 1) ???(phone ??) - ???? Item? ?? (??? phone? ?? PhonePlace?? ??)
        Item item = hit.transform.GetComponent<Item>();
        if (item != null && item.enabled)
        {
            interactPromptUI.SetActive(true);
            if (Input.GetKeyDown(KeyCode.E))
            {

                if (item.itemName == PhonePlace.PhoneItemName)
                    HidePhone(item);
                else if (item.itemName == "box")
                    InteractWithBox(item);
                else if (item.itemName == "newspaper")
                    OpenNewspaper(item);
                else if (HasClueImageFor(item.itemName))
                    OpenClueViewer(item);
                else if (HasVoiceRecorderImageFor(item.itemName))
                    OpenVoiceRecorder(item);
                else if (item.itemName == "diary")
                    OpenDiary(item);
                else if (item.itemName == "amulet")
                    Collect(item);
                else if (item.itemName == "BedPillow")
                    InteractBedPillowForTrueEnding(item);
                else if (IsHandleItem(item.itemName))
                {
                    if (item.itemName == BathroomHandleBadItemName && IsInBadEndingMap())
                    {
                        StartCoroutine(BadEndingBathroomHandleFadeAndResetToDay1());
                        return;
                    }

                    if (item.itemName == BathroomHandleItemName)
                    {
                        int day = SpawnManager.Instance != null ? SpawnManager.Instance.currentDay : 0;

                        if (day == 6)
                        {
                            if (!_hasDay6FadeDone && IsInDay6DimActiveMap())
                            {
                                _hasDay6FadeDone = true;
                                if (objectToActivateOnDay6Handle != null)
                                    objectToActivateOnDay6Handle.SetActive(true);
                                ToggleHandleObject();
                                StartCoroutine(Day6FadeBlackAndBack());
                            }
                            return;
                        }
                    }

                    if (!_hasDoneBathroomHandleFadeOnce)
                        StartCoroutine(ToggleHandleObjectWithFade());
                    else
                        ToggleHandleObject();

                    if (item.itemName == BathroomHandleItemName)
                        _bathroomHandleInteractCount++;

                    if (!_hasDimmedAfterSecondHandle
                        && item.itemName == BathroomHandleItemName
                        && _bathroomHandleInteractCount >= 2
                        && IsInDimActiveMap())
                    {
                        _hasDimmedAfterSecondHandle = true;
                        ApplyDimOverlay();
                    }
                }
                else
                    Collect(item);
            }
            return;
        }

        // 2) phone? ?? ??(PhonePlace) ?? ? E? ?? ???
        PhonePlace phonePlace = hit.transform.GetComponent<PhonePlace>();
        if (phonePlace != null)
        {
            interactPromptUI.SetActive(true);
            if (Input.GetKeyDown(KeyCode.E))
            {
                ShowPhone(phonePlace);
            }
            return;
        }

        interactPromptUI.SetActive(false);
    }

    /// <summary>E키 상호작용 후에도 씬에 남겨둘 아이템 이름 (사라지지 않음)</summary>
    /// <summary>clueImages / voiceRecorderImages에 등록된 아이템은 자동으로 씬에 남음. 여기에는 그 외 남겨둘 아이템만.</summary>
    public static readonly string[] PersistentItemNames = { "bathroom_handle", "bathroom_handle_bad", "newspaper", "diary" };

    /// <summary>이름이 handle(또는 bathroom_handle)인 아이템은 E키로 지정 오브젝트 활성/비활성 토글.</summary>
    public const string BathroomHandleItemName = "bathroom_handle";
    public const string BathroomHandleBadItemName = "bathroom_handle_bad";

    [Header("Box - E키 상호작용 시 box 사라지고 box_glitch 활성화")]
    [Tooltip("box 아이템과 E키 상호작용 시 활성화할 오브젝트 (box_glitch)")]
    public GameObject boxGlitchObject;

    private GameObject _interactedBoxObject;

    [Header("Handle - E키로 표시/숨김 토글")]
    [Tooltip("이름이 handle인 아이템과 E키 상호작용 시 켜졌다 꺼졌다 할 오브젝트들 (복제한 prefab 인스턴스 등 모두 추가)")]
    public List<GameObject> objectsToToggleWithHandle = new List<GameObject>();

    [Header("Handle - 6일차 bathroom_handle 상호작용 시 활성화")]
    [Tooltip("6일차에 bathroom_handle과 E키 상호작용 시 활성화할 오브젝트 (상호작용 전까지 비활성화 상태여야 함)")]
    public GameObject objectToActivateOnDay6Handle;

    [Header("Handle - 화면 페이드 (bathroom_handle E키 시)")]
    [Tooltip("bathroom_handle 상호작용 시 까매졌다 풀리는 효과에 쓸 풀스크린 검정 Image. BedInteraction의 fadeImage와 동일 오브젝트 지정 가능.")]
    public Image bathroomHandleFadeImage;
    [Tooltip("화면이 검게 유지되는 시간(초)")]
    public float bathroomHandleFadeHoldDuration = 3f;
    [Tooltip("페이드 인/아웃에 걸리는 시간(초)")]
    public float bathroomHandleFadeTransitionDuration = 0.5f;

    [Header("Handle - 2회 상호작용 dim (Day4: 즉시 어둡게)")]
    [Tooltip("bathroom_handle 두 번째 E키 상호작용 시 화면을 어둡게 할 Image")]
    public Image dimOverlayImage;
    [Tooltip("어둡게 할 때 Image의 알파값 (0=투명, 1=완전 검정). 0.6 정도면 상당히 어두움")]
    [Range(0f, 1f)]
    public float dimOverlayAlpha = 0.6f;
    [Tooltip("Day4 dim이 작동할 맵의 루트 오브젝트 이름")]
    public string dimActiveMapRootName = "House_Day4";

    [Header("Handle - 2회 상호작용 페이드 (Day6: 서서히 까매짐 → 밝아짐)")]
    [Tooltip("6일차 bathroom_handle 두 번째 상호작용 시 페이드에 사용할 풀스크린 검정 Image (dimOverlayImage 또는 bathroomHandleFadeImage와 같아도 됨)")]
    public Image day6FadeImage;
    [Tooltip("서서히 까매지는 데 걸리는 시간(초)")]
    public float day6FadeInDuration = 1.0f;
    [Tooltip("완전히 까맣게 유지되는 시간(초)")]
    public float day6BlackHoldDuration = 2.0f;
    [Tooltip("다시 밝아지는 데 걸리는 시간(초)")]
    public float day6FadeOutDuration = 1.0f;
    [Tooltip("까맣게 된 동안 비활성화할 오브젝트들")]
    public List<GameObject> day6ObjectsToDeactivate = new List<GameObject>();
    [Tooltip("Day6 dim이 작동할 맵의 루트 오브젝트 이름")]
    public string day6DimActiveMapRootName = "House_Day6";

    [Header("Bad Ending - bathroom_handle 리셋")]
    [Tooltip("Bad Ending 맵으로 판정할 루트 오브젝트/씬 이름. (기본은 HouseSyndromeScene의 root 이름 'Bad_Ending')")]
    public string badEndingMapRootName = "Bad_Ending";
    [Tooltip("Bad Ending에서 day1 스폰으로 이동하기 전 검정 유지 시간(초)")]
    public float badEndingResetBlackHoldDuration = 0.2f;
    [Tooltip("Day1으로 스폰(텔레포트)된 뒤, 이 시간(초) 후에 다시 검정 화면(페이드아웃)을 띄웁니다.")]
    public float badEndingBlackoutDelayAfterSpawn = 5f;
    [Tooltip("Day1 스폰 후 2차 검정 화면을 유지하는 시간(초)")]
    public float badEndingBlackoutHoldDuration = 0.2f;

    private bool _isHandleFading = false;
    private bool _hasDoneBathroomHandleFadeOnce = false;
    private bool _isBadEndingBathroomHandleResetting = false;
    private bool _hasConsumedDay6BathroomHandleInteraction = false;
    private bool _hasDimmedAfterSecondHandle = false;
    private bool _hasDay6FadeDone = false;
    private bool _isDay6Fading = false;
    private int _bathroomHandleInteractCount = 0;
    private int _day6HandleInteractCount = 0;

    IEnumerator ToggleHandleObjectWithFade()
    {
        if (_isHandleFading || bathroomHandleFadeImage == null)
        {
            ToggleHandleObject();
            yield break;
        }
        _isHandleFading = true;
        bathroomHandleFadeImage.gameObject.SetActive(true);
        Color c = bathroomHandleFadeImage.color;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / bathroomHandleFadeTransitionDuration;
            c.a = Mathf.Clamp01(t);
            bathroomHandleFadeImage.color = c;
            yield return null;
        }
        c.a = 1f;
        bathroomHandleFadeImage.color = c;
        ToggleHandleObject();
        yield return new WaitForSeconds(bathroomHandleFadeHoldDuration);
        t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime / bathroomHandleFadeTransitionDuration;
            c.a = Mathf.Clamp01(t);
            bathroomHandleFadeImage.color = c;
            yield return null;
        }
        c.a = 0f;
        bathroomHandleFadeImage.color = c;
        bathroomHandleFadeImage.gameObject.SetActive(false);
        _isHandleFading = false;
        _hasDoneBathroomHandleFadeOnce = true;
    }

    static bool IsHandleItem(string itemName)
    {
        return itemName == BathroomHandleItemName || itemName == BathroomHandleBadItemName;
    }

    bool IsInBadEndingMap()
    {
        if (string.IsNullOrEmpty(badEndingMapRootName))
            return false;

        if (SceneManager.GetActiveScene().name == badEndingMapRootName)
            return true;

        Transform current = transform;
        while (current != null)
        {
            if (current.name == badEndingMapRootName)
                return true;
            current = current.parent;
        }

        if (walkingCamera != null)
        {
            current = walkingCamera.transform;
            while (current != null)
            {
                if (current.name == badEndingMapRootName)
                    return true;
                current = current.parent;
            }
        }

        return false;
    }

    static void SetImageAlpha(Image img, float alpha)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }

    IEnumerator BadEndingBathroomHandleFadeAndResetToDay1()
    {
        if (_isBadEndingBathroomHandleResetting)
            yield break;

        _isBadEndingBathroomHandleResetting = true;

        var spawnManager = SpawnManager.Instance;
        Image fadeImg = spawnManager != null ? spawnManager.screenFadeImage : null;
        if (fadeImg == null)
            fadeImg = bathroomHandleFadeImage;

        float duration = Mathf.Max(0.0001f, bathroomHandleFadeTransitionDuration);

        // 1) Fade to black
        if (fadeImg != null)
        {
            fadeImg.gameObject.SetActive(true);
            SetImageAlpha(fadeImg, 0f);

            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime / duration;
                SetImageAlpha(fadeImg, Mathf.Clamp01(elapsed));
                yield return null;
            }

            SetImageAlpha(fadeImg, 1f);
        }

        yield return new WaitForSeconds(badEndingResetBlackHoldDuration);

        // 2) Teleport to Day1 spawn (no screen fade from SpawnManager side)
        if (spawnManager != null)
            spawnManager.ResetToDay1TeleportOnly(showDayText: true);
        else
            Debug.LogWarning("SpawnManager.Instance를 찾지 못해 Day1 리셋 teleport을 수행하지 못했습니다.");

        // 3) Fade back in (immediately after teleport)
        if (fadeImg != null)
        {
            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime / duration;
                SetImageAlpha(fadeImg, Mathf.Lerp(1f, 0f, Mathf.Clamp01(elapsed)));
                yield return null;
            }

            SetImageAlpha(fadeImg, 0f);
            fadeImg.gameObject.SetActive(false);
        }

        // 4) After spawn: wait, then fade to black again, then fade back in
        if (fadeImg != null && badEndingBlackoutDelayAfterSpawn > 0f)
        {
            yield return new WaitForSeconds(badEndingBlackoutDelayAfterSpawn);

            fadeImg.gameObject.SetActive(true);
            SetImageAlpha(fadeImg, 0f);

            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime / duration;
                SetImageAlpha(fadeImg, Mathf.Clamp01(elapsed));
                yield return null;
            }

            SetImageAlpha(fadeImg, 1f);

            if (badEndingBlackoutHoldDuration > 0f)
                yield return new WaitForSeconds(badEndingBlackoutHoldDuration);

            elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime / duration;
                SetImageAlpha(fadeImg, Mathf.Lerp(1f, 0f, Mathf.Clamp01(elapsed)));
                yield return null;
            }

            SetImageAlpha(fadeImg, 0f);
            fadeImg.gameObject.SetActive(false);
        }

        _isBadEndingBathroomHandleResetting = false;
    }

    void ToggleHandleObject()
    {
        if (objectsToToggleWithHandle == null || objectsToToggleWithHandle.Count == 0) return;
        bool setActive = true;
        for (int i = 0; i < objectsToToggleWithHandle.Count; i++)
        {
            if (objectsToToggleWithHandle[i] != null)
            {
                setActive = !objectsToToggleWithHandle[i].activeSelf;
                break;
            }
        }
        for (int i = 0; i < objectsToToggleWithHandle.Count; i++)
        {
            if (objectsToToggleWithHandle[i] != null)
                objectsToToggleWithHandle[i].SetActive(setActive);
        }

        if (SleepRuleManager.Instance != null)
            SleepRuleManager.Instance.RecordBathroomHandleToggle(setActive);
    }

    void InteractWithBox(Item item)
    {
        _interactedBoxObject = item.gameObject;
        _interactedBoxObject.SetActive(false);
        if (boxGlitchObject != null)
            boxGlitchObject.SetActive(true);
    }

    void OnEnable()
    {
        SpawnManager.OnDayChangedEvent += ResetBoxState;
    }

    void OnDisable()
    {
        SpawnManager.OnDayChangedEvent -= ResetBoxState;
    }

    void ResetBoxState()
    {
        if (_interactedBoxObject != null)
        {
            _interactedBoxObject.SetActive(true);
            _interactedBoxObject = null;
        }
        if (boxGlitchObject != null)
            boxGlitchObject.SetActive(false);
        // 일차가 바뀔 때마다 bathroom_handle 첫 상호작용에서 다시 페이드 인/아웃이 재생되도록 리셋
        _hasDoneBathroomHandleFadeOnce = false;
        _isHandleFading = false;
        _hasConsumedDay6BathroomHandleInteraction = false;
        _hasDimmedAfterSecondHandle = false;
        _bathroomHandleInteractCount = 0;
        _day6HandleInteractCount = 0;
        _hasDay6FadeDone = false;
        _isDay6Fading = false;
        RemoveDimOverlay();
        _bedPillowTrueEndingDone = false;
        _bedPillowInteracted = false;
    }

    bool IsInDimActiveMap()
    {
        if (string.IsNullOrEmpty(dimActiveMapRootName))
            return false;
        GameObject root = GameObject.Find(dimActiveMapRootName);
        return root != null && root.activeInHierarchy;
    }

    void ApplyDimOverlay()
    {
        if (dimOverlayImage == null)
        {
            Debug.LogWarning("[ItemInteraction] dimOverlayImage가 지정되지 않아 화면을 어둡게 할 수 없습니다. Inspector에서 연결하세요.");
            return;
        }
        dimOverlayImage.gameObject.SetActive(true);
        dimOverlayImage.color = new Color(0f, 0f, 0f, dimOverlayAlpha);
        dimOverlayImage.raycastTarget = false;
        Debug.Log($"[ItemInteraction] ApplyDimOverlay — alpha={dimOverlayAlpha}로 화면 어둡게 적용 완료");
    }

    void RemoveDimOverlay()
    {
        if (dimOverlayImage == null) return;
        dimOverlayImage.color = new Color(0f, 0f, 0f, 0f);
        dimOverlayImage.gameObject.SetActive(false);
    }

    bool IsInDay6DimActiveMap()
    {
        if (string.IsNullOrEmpty(day6DimActiveMapRootName))
            return false;
        GameObject root = GameObject.Find(day6DimActiveMapRootName);
        return root != null && root.activeInHierarchy;
    }

    IEnumerator Day6FadeBlackAndBack()
    {
        if (_isDay6Fading) yield break;
        _isDay6Fading = true;

        Image fadeImg = day6FadeImage;
        if (fadeImg == null)
        {
            Debug.LogWarning("[ItemInteraction] day6FadeImage가 지정되지 않아 6일차 페이드를 실행할 수 없습니다.");
            _isDay6Fading = false;
            yield break;
        }

        fadeImg.gameObject.SetActive(true);
        fadeImg.raycastTarget = false;
        SetImageAlpha(fadeImg, 0f);

        float fadeDur = Mathf.Max(0.001f, day6FadeInDuration);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeDur;
            SetImageAlpha(fadeImg, Mathf.Clamp01(t));
            yield return null;
        }
        SetImageAlpha(fadeImg, 1f);

        for (int i = 0; i < day6ObjectsToDeactivate.Count; i++)
        {
            if (day6ObjectsToDeactivate[i] != null)
                day6ObjectsToDeactivate[i].SetActive(false);
        }

        yield return new WaitForSeconds(day6BlackHoldDuration);

        fadeDur = Mathf.Max(0.001f, day6FadeOutDuration);
        t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime / fadeDur;
            SetImageAlpha(fadeImg, Mathf.Clamp01(t));
            yield return null;
        }
        SetImageAlpha(fadeImg, 0f);
        fadeImg.gameObject.SetActive(false);

        _isDay6Fading = false;
    }

    void InteractBedPillowForTrueEnding(Item item)
    {
        // Bad ending 조건 판별용: 아뮬릿을 가지고 있든 없든 베개를 E로 누르면 상호작용한 것으로 간주
        _bedPillowInteracted = true;

        if (collectedItems == null || !collectedItems.Contains("amulet"))
            return;
        if (_bedPillowTrueEndingDone)
            return;
        _bedPillowTrueEndingDone = true;
        if (logText != null)
        {
            logText.text = $"'{item.itemName}'와(과) 상호작용했습니다.\n{item.description}";
            Invoke("ClearLog", 4f);
        }
    }

    void OpenNewspaper(Item item)
    {
        if (newspaperUIRoot != null)
            newspaperUIRoot.SetActive(true);

        _isNewspaperOpen = true;
        Collect(item);
    }

    void CloseNewspaper()
    {
        if (newspaperUIRoot != null)
            newspaperUIRoot.SetActive(false);

        _isNewspaperOpen = false;
    }

    /// <summary>clueImages에 해당 itemName이 있으면 true (clue_1, clue_2, clue_3 등 추가 시 여기만 등록하면 됨)</summary>
    bool HasClueImageFor(string itemName)
    {
        if (string.IsNullOrEmpty(itemName) || clueImages == null) return false;
        foreach (var entry in clueImages)
            if (entry != null && entry.itemName == itemName) return true;
        return false;
    }

    void OpenClueViewer(Item item)
    {
        if (clueViewerImage != null && clueImages != null)
        {
            foreach (var entry in clueImages)
            {
                if (entry != null && entry.itemName == item.itemName && entry.sprite != null)
                {
                    clueViewerImage.sprite = entry.sprite;
                    clueViewerImage.enabled = true;
                    break;
                }
            }
        }
        if (clueViewerUIRoot != null)
            clueViewerUIRoot.SetActive(true);

        _isClueViewerOpen = true;
        Collect(item);
    }

    void CloseClueViewer()
    {
        if (clueViewerUIRoot != null)
            clueViewerUIRoot.SetActive(false);

        _isClueViewerOpen = false;
    }

    bool HasVoiceRecorderImageFor(string itemName)
    {
        if (string.IsNullOrEmpty(itemName) || voiceRecorderImages == null) return false;
        foreach (var entry in voiceRecorderImages)
            if (entry != null && entry.itemName == itemName) return true;
        return false;
    }

    void OpenVoiceRecorder(Item item)
    {
        _voiceRecorderClickClip = null;
        if (voiceRecorderImages != null)
        {
            foreach (var entry in voiceRecorderImages)
            {
                if (entry != null && entry.itemName == item.itemName)
                {
                    _voiceRecorderClickClip = entry.clickSound;
                    if (voiceRecorderImage != null && entry.sprite != null)
                    {
                        voiceRecorderImage.sprite = entry.sprite;
                        voiceRecorderImage.enabled = true;
                    }
                    break;
                }
            }
        }
        if (voiceRecorderImage != null)
            voiceRecorderImage.raycastTarget = true;
        if (voiceRecorderUIRoot != null)
            voiceRecorderUIRoot.SetActive(true);

        _isVoiceRecorderOpen = true;
        IsVoiceRecorderUiOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Collect(item);
    }

    void CloseVoiceRecorder()
    {
        if (voiceRecorderUIRoot != null)
            voiceRecorderUIRoot.SetActive(false);

        _isVoiceRecorderOpen = false;
        IsVoiceRecorderUiOpen = false;
        _voiceRecorderClickClip = null;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>VoiceRecorderPhotoClick(Image)에서 호출합니다.</summary>
    public void OnVoiceRecorderPhotoClicked()
    {
        if (!_isVoiceRecorderOpen || _voiceRecorderClickClip == null)
            return;

        if (voiceRecorderAudioSource != null)
        {
            voiceRecorderAudioSource.PlayOneShot(_voiceRecorderClickClip);
            return;
        }

        Vector3 pos = walkingCamera != null ? walkingCamera.transform.position : Vector3.zero;
        AudioSource.PlayClipAtPoint(_voiceRecorderClickClip, pos);
    }

    void OpenDiary(Item item)
    {
        _diaryCurrentPage = 0;
        if (diaryUIRoot != null)
            diaryUIRoot.SetActive(true);
        if (diaryImage != null && diaryPages != null && diaryPages.Count > 0)
        {
            diaryImage.sprite = diaryPages[0];
            diaryImage.enabled = true;
        }
        if (diaryPageHintText != null)
            diaryPageHintText.gameObject.SetActive(true);
        _isDiaryOpen = true;
        Collect(item);
    }

    void DiaryNextPage()
    {
        if (diaryPages == null || diaryPages.Count == 0) return;
        _diaryCurrentPage++;
        if (_diaryCurrentPage >= diaryPages.Count)
            _diaryCurrentPage = diaryPages.Count - 1;
        if (diaryImage != null && _diaryCurrentPage < diaryPages.Count && diaryPages[_diaryCurrentPage] != null)
            diaryImage.sprite = diaryPages[_diaryCurrentPage];
        if (diaryPageHintText != null && _diaryCurrentPage >= diaryPages.Count - 1)
            diaryPageHintText.gameObject.SetActive(false);
    }

    void CloseDiary()
    {
        if (diaryUIRoot != null)
            diaryUIRoot.SetActive(false);
        if (diaryPageHintText != null)
            diaryPageHintText.gameObject.SetActive(false);

        _isDiaryOpen = false;
    }

    void Collect(Item item)
    {
        collectedItems.Add(item.itemName);

        if (logText != null)
        {
            logText.text = $"'{item.itemName}'??(??) ??????.\n{item.description}";
            Invoke("ClearLog", 4f); // 4?? ?? ??? ????
        }

        bool keepInScene = false;
        for (int i = 0; i < PersistentItemNames.Length; i++)
        {
            if (item.itemName == PersistentItemNames[i])
            {
                keepInScene = true;
                break;
            }
        }
        if (!keepInScene && HasClueImageFor(item.itemName))
            keepInScene = true;
        if (!keepInScene && HasVoiceRecorderImageFor(item.itemName))
            keepInScene = true;
        if (!keepInScene)
            Destroy(item.gameObject);
    }

    [Header("Phone - E? ??? 10? ? ?? ??")]
    public float phoneCapsuleDelay = 10f;
    [Tooltip("????? ? ?? ????. E? phone ?? ? 10? ? ????")]
    public GameObject capsuleToShowAfterPhone;

    Coroutine _phoneCapsuleRoutine;

    void HidePhone(Item phoneItem)
    {
        GameObject go = phoneItem.gameObject;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            r.enabled = false;
        phoneItem.enabled = false;
        go.GetComponent<PhonePlace>().enabled = true;

        if (capsuleToShowAfterPhone != null)
        {
            if (_phoneCapsuleRoutine != null)
                StopCoroutine(_phoneCapsuleRoutine);
            _phoneCapsuleRoutine = StartCoroutine(ShowCapsuleAfterDelay());
        }
    }

    IEnumerator ShowCapsuleAfterDelay()
    {
        yield return new WaitForSeconds(phoneCapsuleDelay);
        if (capsuleToShowAfterPhone != null)
            capsuleToShowAfterPhone.SetActive(true);
        _phoneCapsuleRoutine = null;
    }

    void ShowPhone(PhonePlace phonePlace)
    {
        GameObject go = phonePlace.gameObject;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            r.enabled = true;
        go.GetComponent<Item>().enabled = true;
        phonePlace.enabled = false;
    }

    void ClearLog() { if (logText != null) logText.text = ""; }

    /// <summary> amulet을 먼저 E키로 줍고, 이어서 BedPillow를 E키로 상호작용했을 때만 true. </summary>
    public bool IsTrueEndingSpawnReady()
    {
        return collectedItems != null
            && collectedItems.Contains("amulet")
            && _bedPillowTrueEndingDone;
    }

    /// <summary>amulet을 E로 상호작용(수집)했는지 여부</summary>
    public bool HasInteractedAmulet()
    {
        return collectedItems != null && collectedItems.Contains("amulet");
    }

    /// <summary>BedPillow를 E로 상호작용했는지 여부</summary>
    public bool HasInteractedBedPillow()
    {
        return _bedPillowInteracted;
    }
}