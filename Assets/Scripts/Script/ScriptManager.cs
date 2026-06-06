using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public struct TimedDialogueEntry
{
    [Tooltip("두 자릿수 ID (10~99)")]
    public int id;
    [Tooltip("게임 시작 후 몇 초 뒤에 표시할지")]
    public float delaySeconds;
}

[DefaultExecutionOrder(200)]
public class ScriptManager : MonoBehaviour
{
    public static ScriptManager Instance { get; private set; }

    [Header("Chat Data")]
    [SerializeField]
    private ChatData chatData;

    [Header("Dialogue UI")]
    [SerializeField]
    private TextMeshProUGUI dialogueText;

    [SerializeField]
    private GameObject dialogueBackground;

    [Header("Dialogue Timing")]
    [Tooltip("날짜 변경 후 몇 초 뒤에 대사를 보여줄지")]
    [SerializeField]
    private float showDelayAfterDayChanged = 0.5f;

    [Tooltip("대사가 화면에 유지되는 시간")]
    [SerializeField]
    private float dialogueDisplayDuration = 3f;

    [Header("ID Settings")]
    [Tooltip("구글 시트 id가 0부터 시작하면 true. Day1=id0, Day2=id1")]
    [SerializeField]
    private bool chatIdStartsFromZero = true;

    [Header("Timed Dialogue (두 자릿수 ID: 10~99)")]
    [Tooltip("게임 시작 후 지정한 시간이 지나면 자동으로 표시됩니다.")]
    [SerializeField]
    private List<TimedDialogueEntry> timedDialogues = new List<TimedDialogueEntry>();

    private Dictionary<int, string> chatDictionary = new Dictionary<int, string>();

    private Coroutine delayRoutine;
    private Coroutine displayRoutine;
    private List<Coroutine> timedRoutines = new List<Coroutine>();

    private int lastShownDay = -1;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        MakeChatDictionary();
        HideDialogue();
    }

    private void OnEnable()
    {
        SpawnManager.OnDayChangedEvent += OnDayChanged;
    }

    private void OnDisable()
    {
        SpawnManager.OnDayChangedEvent -= OnDayChanged;
    }

    private IEnumerator Start()
    {
        // SpawnManager가 currentDay를 세팅하고 스폰 처리할 시간을 기다림
        yield return null;

        // 한 자릿수 ID: 현재 날짜에 맞는 대사 표시
        ShowDialogueForCurrentDay();

        // 두 자릿수 ID: 현재 날짜에 해당하는 타이머 시작
        StartTimedDialoguesForCurrentDay();
    }

    private void MakeChatDictionary()
    {
        chatDictionary.Clear();

        if (chatData == null)
        {
            Debug.LogError("ScriptManager: ChatData가 연결되지 않았습니다.");
            return;
        }

        foreach (Chat chat in chatData.chatDataList)
        {
            chatDictionary[chat.id] = chat.content;
        }

        Debug.Log($"ScriptManager: 대사 Dictionary 생성 완료 - {chatDictionary.Count}개");
    }

    // ──────────────────────────────────────────────
    // 한 자릿수 ID (0~9): 날짜에 매치되는 대사
    // ──────────────────────────────────────────────

    private void OnDayChanged()
    {
        if (delayRoutine != null)
            StopCoroutine(delayRoutine);

        // 이전 날의 남은 타이머 취소
        StopAllTimedRoutines();

        delayRoutine = StartCoroutine(ShowDialogueAfterDelay());
    }

    private IEnumerator ShowDialogueAfterDelay()
    {
        yield return new WaitForSeconds(showDelayAfterDayChanged);
        ShowDialogueForCurrentDay();

        // 새 날의 두 자릿수 타이머 시작
        StartTimedDialoguesForCurrentDay();

        delayRoutine = null;
    }

    public void ShowDialogueForCurrentDay()
    {
        if (SpawnManager.Instance == null)
        {
            Debug.LogError("ScriptManager: SpawnManager.Instance를 찾을 수 없습니다.");
            return;
        }

        int currentDay = SpawnManager.Instance.GetCurrentDay();

        if (lastShownDay == currentDay)
            return;

        int id = GetChatIdByDay(currentDay);

        bool success = ShowDialogueById(id);

        if (success)
            lastShownDay = currentDay;
    }

    private int GetChatIdByDay(int currentDay)
    {
        return chatIdStartsFromZero ? currentDay - 1 : currentDay;
    }

    // ──────────────────────────────────────────────
    // 두 자릿수 ID (10~89): 날짜별 시간 설정 기반 대사
    // 10~19 → Day1, 20~29 → Day2, ..., 80~89 → Day8
    // ──────────────────────────────────────────────

    private int GetTargetDayFromTimedId(int id) => id / 10;

    private void StartTimedDialoguesForCurrentDay()
    {
        if (SpawnManager.Instance == null) return;

        int currentDay = SpawnManager.Instance.GetCurrentDay();

        foreach (TimedDialogueEntry entry in timedDialogues)
        {
            if (entry.id < 10 || entry.id > 89)
            {
                Debug.LogWarning($"ScriptManager: TimedDialogue id {entry.id}는 10~89 범위여야 합니다. 무시됩니다.");
                continue;
            }

            if (GetTargetDayFromTimedId(entry.id) != currentDay)
                continue;

            Coroutine c = StartCoroutine(ShowTimedDialogue(entry));
            timedRoutines.Add(c);
        }
    }

    private void StopAllTimedRoutines()
    {
        foreach (Coroutine c in timedRoutines)
        {
            if (c != null)
                StopCoroutine(c);
        }
        timedRoutines.Clear();
    }

    private IEnumerator ShowTimedDialogue(TimedDialogueEntry entry)
    {
        yield return new WaitForSeconds(entry.delaySeconds);
        ShowDialogueById(entry.id);
    }

    // ──────────────────────────────────────────────
    // 세 자릿수 ID (100~999): 오브젝트 상호작용 시
    // 외부 스크립트에서 ScriptManager.Instance.ShowInteractionDialogue(id) 호출
    // ──────────────────────────────────────────────

    /// <summary>
    /// 오브젝트 상호작용 시 호출. id는 세 자릿수(100~999)를 사용합니다.
    /// </summary>
    public void ShowInteractionDialogue(int id)
    {
        if (id < 100 || id > 999)
        {
            Debug.LogWarning($"ScriptManager: ShowInteractionDialogue id {id}는 세 자릿수(100~999)여야 합니다.");
            return;
        }

        ShowDialogueById(id);
    }

    // ──────────────────────────────────────────────
    // 공통 표시 로직
    // ──────────────────────────────────────────────

    public bool ShowDialogueById(int id)
    {
        if (dialogueText == null)
        {
            Debug.LogError("ScriptManager: Dialogue Text가 연결되지 않았습니다.");
            return false;
        }

        if (chatData == null)
        {
            Debug.LogError("ScriptManager: ChatData가 연결되지 않았습니다.");
            return false;
        }

        if (chatDictionary.Count == 0)
            MakeChatDictionary();

        if (!chatDictionary.TryGetValue(id, out string content))
        {
            Debug.LogWarning($"ScriptManager: id {id}에 해당하는 대사가 없습니다.");
            HideDialogue();
            return false;
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            HideDialogue();
            return false;
        }

        if (displayRoutine != null)
            StopCoroutine(displayRoutine);

        displayRoutine = StartCoroutine(DisplayDialogueRoutine(id, content));
        return true;
    }

    private IEnumerator DisplayDialogueRoutine(int id, string content)
    {
        ShowDialogueUI();

        dialogueText.text = content;

        Debug.Log($"ScriptManager: ID {id}, Content: {content}");

        yield return new WaitForSeconds(dialogueDisplayDuration);

        HideDialogue();

        displayRoutine = null;
    }

    private void ShowDialogueUI()
    {
        if (dialogueBackground != null)
            dialogueBackground.SetActive(true);

        if (dialogueText != null)
            dialogueText.gameObject.SetActive(true);
    }

    private void HideDialogue()
    {
        if (dialogueText != null)
        {
            dialogueText.text = "";
            dialogueText.gameObject.SetActive(false);
        }

        if (dialogueBackground != null)
            dialogueBackground.SetActive(false);
    }
}
