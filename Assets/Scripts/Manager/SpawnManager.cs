using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using DayNightSystem;

/// <summary>
/// 침대에서 깨어났을 때의 스폰 위치·날짜 진행·Day UI만 담당.
/// 침대 상호작용(레이캐스트, E/I키)은 BedInteraction이 담당.
/// </summary>
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

    [Header("Day-Night Cycle")]
    public DayNightSystem.DayNightManager dayNightCycle;

    [Header("Day Map (1~7일차별 맵 활성화)")]
    public DayMapManager dayMapManager;

    [Header("Clocks (1~7일차별 시계)")]
    [Tooltip("Element 0 = Day1 시계, Element 1 = Day2 시계, ... Element 6 = Day7 시계")]
    public Clock[] clocksByDay = new Clock[7];

    /// <summary> 현재 밤인지. DayNightManager 시간 기준 (예: 20시~6시). </summary>
    public bool IsNightTime =>
        dayNightCycle != null && dayNightCycle.IsWithinTime(20f, 6f);

    private CanvasGroup dayTextCanvasGroup;
    private bool isSleeping = false;

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

        OnDayChanged(); // 현재 날짜에 맞는 맵 활성화
        ShowDayUI();    // "Day X" UI 표시
        TeleportPlayerToSpawn(); // 침대 위치로 플레이어 이동
    }

    /// <summary>
    /// 밤몽에서 깨어날 때 BedInteraction이 호출. 날짜 진행 + 스폰 이동 + Day UI + 낮 리셋.
    /// </summary>
    // [핵심] 가위 탈출 성공 시 호출되는 함수
    public void AdvanceDayFromNightmare()
    {
        // 1. 날짜 증가 (최대 7일)
        currentDay++;
        if (currentDay > 7) currentDay = 7;

        // 2. 난이도 매니저(Persistent)에 날짜 저장
        if (DifficultyManager.Instance != null)
        {
            DifficultyManager.Instance.currentDay = currentDay;
        }

        // 3. 밤 상태 종료 (시간을 낮으로 되돌림)
        if (dayNightCycle != null)
        {
            dayNightCycle.ResetTime();
        }

        // 4. 귀신 제거
        if (GhostManager.Instance != null)
        {
            GhostManager.Instance.ClearGhost();
        }

        // 5. 씬 재시작 (모든 오브젝트 상태 리셋 및 다음 날 맵 로드)
        // 이 방식이 가장 깔끔하게 다음 날로 넘어가는 방법입니다.
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

        TeleportPlayerToSpawn();
        ShowDayUI();
        OnDayChanged();
        if (dayNightCycle != null) dayNightCycle.ResetTime();
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

        TeleportPlayerToSpawn();
        ShowDayUI();
        OnDayChanged();
        if (dayNightCycle != null) dayNightCycle.ResetTime();
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

    void ShowDayUI()
    {
        if (dayText == null) return;
        dayText.text = $"Day {currentDay}";
        StartCoroutine(ShowDayTextRoutine());
    }

    IEnumerator ShowDayTextRoutine()
    {
        dayText.gameObject.SetActive(true);

        if (dayTextCanvasGroup != null)
        {
            dayTextCanvasGroup.alpha = 0f;
            while (dayTextCanvasGroup.alpha < 1f)
            {
                dayTextCanvasGroup.alpha += Time.deltaTime * fadeSpeed;
                yield return null;
            }
            dayTextCanvasGroup.alpha = 1f;
        }

        yield return new WaitForSeconds(displayDuration);

        if (dayTextCanvasGroup != null)
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

    protected virtual void OnDayChanged()
    {
        if (dayMapManager != null)
            dayMapManager.RefreshMapsForCurrentDay();

        bool resetDone = false;

        if (clocksByDay != null &&
            currentDay >= 1 &&
            currentDay <= clocksByDay.Length)
        {
            var clockForDay = clocksByDay[currentDay - 1];
            if (clockForDay != null)
            {
                clockForDay.ResetClockForNewDay();
                resetDone = true;
            }
        }

        // 배열에 할당 안 했거나 null이면 안전하게 씬의 모든 Clock 리셋
        if (!resetDone)
        {
            var clocks = FindObjectsOfType<Clock>(true);
            foreach (var clock in clocks)
                clock.ResetClockForNewDay();
        }
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
