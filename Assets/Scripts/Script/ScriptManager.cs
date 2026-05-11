using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[DefaultExecutionOrder(200)]
public class ScriptManager : MonoBehaviour
{
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

    private Dictionary<int, string> chatDictionary = new Dictionary<int, string>();

    private Coroutine delayRoutine;
    private Coroutine displayRoutine;

    private int lastShownDay = -1;

    private void Awake()
    {
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

        ShowDialogueForCurrentDay();
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
            // 같은 id가 있으면 마지막 값으로 덮어씀
            chatDictionary[chat.id] = chat.content;
        }

        Debug.Log($"ScriptManager: 대사 Dictionary 생성 완료 - {chatDictionary.Count}개");
    }

    private void OnDayChanged()
    {
        if (delayRoutine != null)
        {
            StopCoroutine(delayRoutine);
        }

        delayRoutine = StartCoroutine(ShowDialogueAfterDelay());
    }

    private IEnumerator ShowDialogueAfterDelay()
    {
        yield return new WaitForSeconds(showDelayAfterDayChanged);

        ShowDialogueForCurrentDay();

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
        {
            return;
        }

        int id = GetChatIdByDay(currentDay);

        bool success = ShowDialogueById(id);

        if (success)
        {
            lastShownDay = currentDay;
        }
    }

    private int GetChatIdByDay(int currentDay)
    {
        if (chatIdStartsFromZero)
        {
            return currentDay - 1;
        }

        return currentDay;
    }

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
        {
            MakeChatDictionary();
        }

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
        {
            StopCoroutine(displayRoutine);
        }

        displayRoutine = StartCoroutine(DisplayDialogueRoutine(id, content));
        return true;
    }

    private IEnumerator DisplayDialogueRoutine(int id, string content)
    {
        ShowDialogueUI();

        dialogueText.text = content;

        Debug.Log($"ScriptManager: Day {id + 1}, ID {id}, Content: {content}");

        yield return new WaitForSeconds(dialogueDisplayDuration);

        HideDialogue();

        displayRoutine = null;
    }

    private void ShowDialogueUI()
    {
        if (dialogueBackground != null)
        {
            dialogueBackground.SetActive(true);
        }

        if (dialogueText != null)
        {
            dialogueText.gameObject.SetActive(true);
        }
    }

    private void HideDialogue()
    {
        if (dialogueText != null)
        {
            dialogueText.text = "";
            dialogueText.gameObject.SetActive(false);
        }

        if (dialogueBackground != null)
        {
            dialogueBackground.SetActive(false);
        }
    }
}