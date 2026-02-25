using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

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
    public DayNightCycle dayNightCycle;

    [Header("Day Map (1~7일차별 맵 활성화)")]
    public DayMapManager dayMapManager;

    /// <summary> 현재 밤인지. DayNightCycle에서 조회. </summary>
    public bool IsNightTime => dayNightCycle != null && dayNightCycle.IsNightTime;

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
        if (dayText != null)
        {
            dayTextCanvasGroup = dayText.GetComponent<CanvasGroup>();
            if (dayTextCanvasGroup == null)
                dayTextCanvasGroup = dayText.gameObject.AddComponent<CanvasGroup>();
        }

        ShowDayUI();
        if (dayMapManager != null)
            dayMapManager.RefreshMapsForCurrentDay();
    }

    /// <summary>
    /// 밤몽에서 깨어날 때 BedInteraction이 호출. 날짜 진행 + 스폰 이동 + Day UI + 낮 리셋.
    /// </summary>
    public void AdvanceDayFromNightmare()
    {
        if (isSleeping) return;

        isSleeping = true;
        currentDay++;

        if (DifficultyManager.Instance != null)
            DifficultyManager.Instance.currentDay = currentDay;

        TeleportPlayerToSpawn();
        ShowDayUI();
        OnDayChanged();
        if (dayNightCycle != null) dayNightCycle.ResetToDay();
        if (SleepRuleManager.Instance != null) SleepRuleManager.Instance.ResetRule();

        isSleeping = false;
        Debug.Log($"<color=cyan>Day {currentDay} 시작 (침대에서 깨어남)</color>");
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
