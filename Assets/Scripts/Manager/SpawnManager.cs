using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 침대에서 깨어났을 때의 스폰 위치·날짜 진행·Day UI만 담당.
/// 침대 상호작용(레이캐스트, E/I키)은 BedInteraction이 담당.
/// </summary>
[DefaultExecutionOrder(100)]
public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [Header("Spawn Settings")]
    public int currentDay = 1;

    [Header("Spawn (침대에서 깨어났을 때)")]
    public Transform[] spawnPoints;  // Day1 = index 0, Day2 = index 1, ...
    public Transform player;         // Walking 플레이어 Transform (스폰 이동 대상)

    [Header("Day UI")]
    public TextMeshProUGUI dayText;
    public float displayDuration = 2f;
    public float fadeSpeed = 1f;

    [Header("Spawn Fullscreen Fade")]
    [Tooltip("스폰·게임 시작 시 화면 전체 검정 페이드. BedInteraction의 fadeImage와 동일 오브젝트를 연결해도 됩니다.")]
    public Image screenFadeImage;
    [Tooltip("검정 화면을 유지한 뒤, 이 시간(초)이 지나면 검정이 걷힙니다.")]
    public float spawnBlackHoldDuration = 5f;

    [Header("Day-Night Cycle")]
    public DayNightCycle dayNightCycle;

    [Header("Day Map (1~7일차별 맵 활성화)")]
    public DayMapManager dayMapManager;

    [Header("Spawn Dialogue")]
    [Tooltip("스폰 시 검정·Day UI 인트로가 끝난 뒤 DialogueOnInteract.TryInteract()로 대사를 띄웁니다. 빈 오브젝트에 DialogueOnInteract만 붙여 메시지·표시 시간을 설정하고 여기에 연결하세요.")]
    public DialogueOnInteract spawnDialogue;

    /// <summary> 현재 밤인지. DayNightCycle에서 조회. </summary>
    public bool IsNightTime => dayNightCycle != null && dayNightCycle.IsNightTime;

    private CanvasGroup dayTextCanvasGroup;
    private bool isSleeping = false;
    private Coroutine spawnIntroRoutine;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        // [추가] 시작할 때 난이도 매니저의 날짜를 가져와서 동기화합니다.
        if (DifficultyManager.Instance != null)
        {
            currentDay = DifficultyManager.Instance.currentDay;
        }

        if (Instance == null) Instance = this;

        // UI 및 맵 초기화
        if (dayText != null) dayTextCanvasGroup = dayText.GetComponent<CanvasGroup>();

        PrepareSpawnScreenBlack();

        OnDayChanged(); // 현재 날짜에 맞는 맵 활성화
        TeleportPlayerToSpawn(); // 검정 유지 중 이동 (위치 스냅 가림)
        ShowDayUI();    // Day 텍스트 + 화면 페이드
    }

    /// <summary>
    /// 밤몽에서 깨어날 때 BedInteraction이 호출. 날짜 진행 + 스폰 이동 + Day UI + 낮 리셋.
    /// </summary>
    // [핵심] 가위 탈출 성공 시 호출되는 함수
    public void AdvanceDayFromNightmare()
    {
        // 1. 현재 날짜를 먼저 가져옴
        int nextDay = currentDay + 1;
        if (nextDay > 7) nextDay = 7;

        // 2. 난이도 매니저(Instance)에 먼저 저장 (가장 중요)
        if (DifficultyManager.Instance != null)
        {
            DifficultyManager.Instance.currentDay = nextDay;
            Debug.Log($"<color=cyan>날짜 저장 성공: Day {DifficultyManager.Instance.currentDay}</color>");
        }
        else
        {
            Debug.LogError("DifficultyManager 인스턴스를 찾을 수 없습니다!");
        }

        // 3. 밤 상태 리셋 및 귀신 제거
        if (dayNightCycle != null) dayNightCycle.ResetToDay();
        if (GhostManager.Instance != null) GhostManager.Instance.ClearGhost();

        // 4. 씬 재시작
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// 개발용: 지정한 일차로 이동. 숫자키 스킵 등에서 호출.
    /// </summary>
    public void GoToDay(int day)
    {
        if (isSleeping) return;
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        int d = Mathf.Clamp(day, 1, spawnPoints.Length);
        isSleeping = true;
        currentDay = d;

        if (DifficultyManager.Instance != null)
            DifficultyManager.Instance.currentDay = currentDay;

        PrepareSpawnScreenBlack();
        TeleportPlayerToSpawn();
        ShowDayUI();
        OnDayChanged();
        if (dayNightCycle != null) dayNightCycle.ResetToDay();
        if (SleepRuleManager.Instance != null) SleepRuleManager.Instance.ResetRule();

        isSleeping = false;
        Debug.Log($"<color=yellow>[Dev] Day {currentDay}로 이동</color>");
    }

    /// <summary>
    /// O키 등으로 Day 1로 돌아갈 때 호출. 날짜 1로 고정 후 Day1 스폰 위치로 이동 + Day UI + 낮 리셋.
    /// </summary>
    public void ResetToDay1()
    {
        if (isSleeping) return;

        isSleeping = true;
        currentDay = 1;

        if (DifficultyManager.Instance != null)
            DifficultyManager.Instance.currentDay = currentDay;

        PrepareSpawnScreenBlack();
        TeleportPlayerToSpawn();
        ShowDayUI();
        OnDayChanged();
        if (dayNightCycle != null) dayNightCycle.ResetToDay();
        if (SleepRuleManager.Instance != null) SleepRuleManager.Instance.ResetRule();

        isSleeping = false;
        Debug.Log("<color=cyan>Day 1로 리셋</color>");
    }

    /// <summary>
    /// 현재 날짜에 맞는 스폰 위치로 플레이어 이동. AdvanceDayFromNightmare 내부에서 호출.
    /// </summary>
    public void TeleportPlayerToSpawn()
    {
        if (player == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("DayManager: Player 또는 SpawnPoints가 설정되지 않았습니다.");
            return;
        }

        int spawnIndex = Mathf.Clamp(currentDay - 1, 0, spawnPoints.Length - 1);
        Transform target = spawnPoints[spawnIndex];
        if (target == null)
        {
            Debug.LogWarning($"Day {currentDay} 스폰 포인트가 null입니다.");
            return;
        }

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            player.position = target.position;
            player.rotation = target.rotation;
            cc.enabled = true;
        }
        else
        {
            player.position = target.position;
            player.rotation = target.rotation;
        }

        Debug.Log($"<color=green>Day {currentDay}: {target.name}으로 스폰</color>");
    }

    void PrepareSpawnScreenBlack()
    {
        if (screenFadeImage == null) return;
        screenFadeImage.gameObject.SetActive(true);
        Color c = screenFadeImage.color;
        c.a = 1f;
        screenFadeImage.color = c;
    }

    static void SetImageAlpha(Image img, float a)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = a;
        img.color = c;
    }

    void ShowDayUI()
    {
        if (spawnIntroRoutine != null)
        {
            StopCoroutine(spawnIntroRoutine);
            spawnIntroRoutine = null;
        }

        if (dayText == null && screenFadeImage == null && spawnDialogue == null)
            return;

        spawnIntroRoutine = StartCoroutine(SpawnIntroRoutine());
    }

    IEnumerator SpawnIntroRoutine()
    {
        bool useScreen = screenFadeImage != null;
        bool useDay = dayText != null;

        if (useScreen)
        {
            screenFadeImage.gameObject.SetActive(true);
            SetImageAlpha(screenFadeImage, 1f);
            yield return new WaitForSeconds(spawnBlackHoldDuration);
            while (screenFadeImage.color.a > 0f)
            {
                SetImageAlpha(screenFadeImage, Mathf.Max(0f, screenFadeImage.color.a - Time.deltaTime * fadeSpeed));
                yield return null;
            }

            SetImageAlpha(screenFadeImage, 0f);
            screenFadeImage.gameObject.SetActive(false);
        }

        if (useDay)
            PrepareDayTextForIntro();
        if (spawnDialogue != null)
            spawnDialogue.TryInteract();
        if (useDay)
            yield return DayTextFadeInHoldFadeOutRoutine();

        spawnIntroRoutine = null;
    }

    void PrepareDayTextForIntro()
    {
        if (dayText == null) return;
        dayText.text = $"Day {currentDay}";
        dayText.gameObject.SetActive(true);
        if (dayTextCanvasGroup != null)
            dayTextCanvasGroup.alpha = 0f;
    }

    IEnumerator DayTextFadeInHoldFadeOutRoutine()
    {
        if (dayText == null) yield break;

        bool canFade = dayTextCanvasGroup != null;

        if (canFade)
        {
            while (dayTextCanvasGroup.alpha < 1f)
            {
                dayTextCanvasGroup.alpha += Time.deltaTime * fadeSpeed;
                yield return null;
            }

            dayTextCanvasGroup.alpha = 1f;
        }

        yield return new WaitForSeconds(displayDuration);

        if (canFade)
        {
            while (dayTextCanvasGroup.alpha > 0f)
            {
                dayTextCanvasGroup.alpha -= Time.deltaTime * fadeSpeed;
                yield return null;
            }

            dayTextCanvasGroup.alpha = 0f;
        }

        dayText.gameObject.SetActive(false);
    }

    /// <summary> 일차가 변경될 때 호출됨. GoToDay, ResetToDay1, AdvanceDayFromNightmare(씬 로드 후) 등. </summary>
    public static event System.Action OnDayChangedEvent;

    protected virtual void OnDayChanged()
    {
        if (dayMapManager != null)
            dayMapManager.RefreshMapsForCurrentDay();
        OnDayChangedEvent?.Invoke();
    }

    public int GetCurrentDay()
    {
        return currentDay;
    }

    public void ReturnToPreviousDay()
    {
        //날짜를 1일 감소(최소 1일 유지)
        currentDay = Mathf.Max(1, currentDay - 1);

        //현재 날짜 데이터를 저장하고 씬을 다시 로드
        SaveDayData();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void SaveDayData()
    {
        //DifficultyManager 등 연관된 데이터도 함께 동기화
        if(DifficultyManager.Instance !=null)
        {
            DifficultyManager.Instance.currentDay= currentDay;
        }
    }
}
