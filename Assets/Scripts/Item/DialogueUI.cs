using System.Collections;
using TMPro;
using UnityEngine;

public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    [Header("UI")]
    [Tooltip("대사를 표시할 UI 텍스트(TMP).")]
    public TMP_Text dialogueText;
    [Tooltip("대사 UI 루트(패널). 비워두면 이 오브젝트를 사용합니다.")]
    public GameObject dialogueRoot;

    [Header("Behavior")]
    [Tooltip("게임 시작 시 대사 UI를 숨깁니다.")]
    public bool hideOnStart = true;
    [Tooltip("Show() 호출 시 기본으로 유지할 시간(초). 0 이하면 자동으로 지우지 않음.")]
    public float defaultDurationSeconds = 3f;
    [Tooltip("Show() 호출 시 기존 텍스트를 덮어쓸지 여부.")]
    public bool overwriteExisting = true;

    Coroutine _hideRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BootstrapAfterSceneLoad()
    {
        // Script 패널이 처음부터 비활성화되어 Awake가 안 도는 경우를 대비해,
        // 씬 로드 후 비활성 오브젝트까지 포함해서 DialogueUI를 찾아 Instance를 설정합니다.
        EnsureInstance();
    }

    public static DialogueUI EnsureInstance()
    {
        if (Instance != null) return Instance;

        // Resources.FindObjectsOfTypeAll은 비활성 오브젝트도 찾습니다.
        var all = Resources.FindObjectsOfTypeAll<DialogueUI>();
        if (all == null || all.Length == 0) return null;

        // 실제 씬에 존재하는 오브젝트만 우선 선택
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].gameObject.scene.IsValid())
            {
                Instance = all[i];
                if (Instance.dialogueRoot == null) Instance.dialogueRoot = Instance.gameObject;
                return Instance;
            }
        }

        Instance = all[0];
        if (Instance != null && Instance.dialogueRoot == null) Instance.dialogueRoot = Instance.gameObject;
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (dialogueRoot == null) dialogueRoot = gameObject;
        if (hideOnStart) Hide();
    }

    void OnEnable()
    {
        if (Instance == null) Instance = this;
        if (dialogueRoot == null) dialogueRoot = gameObject;
    }

    public void Show(string message)
    {
        Show(message, defaultDurationSeconds);
    }

    public void Show(string message, float durationSeconds)
    {
        if (dialogueText == null) return;

        if (_hideRoutine != null)
        {
            StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }

        if (dialogueRoot != null && !dialogueRoot.activeSelf)
            dialogueRoot.SetActive(true);

        if (overwriteExisting)
            dialogueText.text = message ?? "";
        else
            dialogueText.text = (dialogueText.text ?? "") + (message ?? "");

        if (durationSeconds > 0f)
            _hideRoutine = StartCoroutine(HideAfter(durationSeconds));
    }

    public void Hide()
    {
        if (dialogueText == null) return;
        if (_hideRoutine != null)
        {
            StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }
        dialogueText.text = "";
        if (dialogueRoot != null && dialogueRoot.activeSelf)
            dialogueRoot.SetActive(false);
    }

    IEnumerator HideAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Hide();
    }
}

