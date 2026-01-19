using UnityEngine;
using TMPro;
using System.Collections;
using System;

public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }

    [Header("Day Settings")]
    public int currentDay = 1;

    [Header("UI Settings")]
    public TextMeshProUGUI dayText;
    public float displayDuration = 2f;
    public float fadeSpeed = 1f;

    [Header("Bed Interaction")]
    public float interactionDistance = 2.5f;
    public KeyCode interactKey = KeyCode.E;

    [Header("Sleep Transition")]
    public GameObject fadeScreen;          // 전체 화면 페이드용 (검은색 Image + CanvasGroup)
    public Transform spawnPoint;            // 잠에서 깨어날 위치
    public Transform player;                // 플레이어 Transform
    public float sleepFadeDuration = 1f;    // 페이드 시간

    [Header("Day-Night Cycle")]
    public DayNightEventReceiver dayNightReceiver;
    
    // 현재 시간대 상태
    public bool IsNightTime { get; private set; } = false;
    
    // 밤이 되었을 때 발생하는 이벤트
    public event Action OnNightStarted;

    private Camera mainCam;
    private CanvasGroup dayTextCanvasGroup;
    private CanvasGroup fadeScreenCanvasGroup;
    private int bedLayerMask;

    private bool showInteractMsg;
    private GUIStyle guiStyle;
    private bool isSleeping = false;  // 수면 전환 중인지 체크

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        mainCam = Camera.main;

        // InteractRaycast 레이어 설정 (MoveObjectController와 동일한 방식)
        LayerMask interactLayer = LayerMask.NameToLayer("InteractRaycast");
        bedLayerMask = 1 << interactLayer.value;

        if (dayText != null)
        {
            dayTextCanvasGroup = dayText.GetComponent<CanvasGroup>();
            if (dayTextCanvasGroup == null)
            {
                dayTextCanvasGroup = dayText.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (fadeScreen != null)
        {
            fadeScreenCanvasGroup = fadeScreen.GetComponent<CanvasGroup>();
            if (fadeScreenCanvasGroup == null)
            {
                fadeScreenCanvasGroup = fadeScreen.AddComponent<CanvasGroup>();
            }
        }

        SetupGui();

        // 게임 시작 시 첫째 날 표시
        ShowDayUI();
        
        // 낮-밤 사이클 시작
        StartDayNightCycle();
    }

    void Update()
    {
        CheckBedInteraction();
    }

    void CheckBedInteraction()
    {
        if (mainCam == null) return;

        // 화면 중앙에서 레이캐스트 (커서가 잠겨있으므로)
        Vector3 rayOrigin = mainCam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, mainCam.transform.forward, out hit, interactionDistance, bedLayerMask))
        {
            // Bed 태그를 가진 오브젝트인지 확인
            if (hit.collider.CompareTag("Bed"))
            {
                showInteractMsg = true;

                // E키 또는 마우스 왼쪽 클릭으로 상호작용
                if (Input.GetKeyUp(interactKey) || Input.GetButtonDown("Fire1"))
                {
                    AdvanceDay();
                }
            }
            else
            {
                showInteractMsg = false;
            }
        }
        else
        {
            showInteractMsg = false;
        }
    }

    public void AdvanceDay()
    {
        if (!IsNightTime)
        {
            Debug.Log("<color=yellow>아직 밤이 아닙니다. 밤이 되면 잠을 잘 수 있습니다.</color>");
            return;
        }

        if (isSleeping) return;  // 이미 수면 중이면 무시

        StartCoroutine(SleepTransitionRoutine());
    }

    IEnumerator SleepTransitionRoutine()
    {
        isSleeping = true;

        // 페이드 아웃
        yield return StartCoroutine(FadeRoutine(0f, 1f));

        // 플레이어를 스폰 포인트로 이동
        TeleportPlayerToSpawn();

        // 날짜 진행
        currentDay++;
        IsNightTime = false;
        Debug.Log($"<color=cyan>Day {currentDay} 시작!</color>");
        
        // Day UI 표시
        ShowDayUI();
        OnDayChanged();
        
        // 다음 날 사이클 시작
        StartDayNightCycle();

        // 페이드 인
        yield return StartCoroutine(FadeRoutine(1f, 0f));

        isSleeping = false;
    }

    IEnumerator FadeRoutine(float startAlpha, float endAlpha)
    {
        if (fadeScreen == null || fadeScreenCanvasGroup == null) yield break;

        fadeScreen.SetActive(true);
        fadeScreenCanvasGroup.alpha = startAlpha;

        float elapsed = 0f;
        while (elapsed < sleepFadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeScreenCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / sleepFadeDuration);
            yield return null;
        }

        fadeScreenCanvasGroup.alpha = endAlpha;

        // 완전히 투명해지면 비활성화
        if (endAlpha <= 0f)
        {
            fadeScreen.SetActive(false);
        }
    }

    void TeleportPlayerToSpawn()
    {
        if (player == null || spawnPoint == null)
        {
            Debug.LogWarning("Player 또는 SpawnPoint가 설정되지 않았습니다.");
            return;
        }

        // CharacterController가 있으면 비활성화 후 이동 (직접 위치 변경 시 필요)
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            player.position = spawnPoint.position;
            player.rotation = spawnPoint.rotation;
            cc.enabled = true;
        }
        else
        {
            player.position = spawnPoint.position;
            player.rotation = spawnPoint.rotation;
        }

        Debug.Log($"<color=green>플레이어가 {spawnPoint.name}으로 이동했습니다.</color>");
    }

    #region Day-Night Cycle

    /// <summary>
    /// 낮-밤 사이클 애니메이션 시작
    /// </summary>
    public void StartDayNightCycle()
    {
        if (dayNightReceiver != null)
        {
            dayNightReceiver.RestartAnimation();
        }
        Debug.Log("<color=yellow>낮-밤 사이클 시작</color>");
    }

    /// <summary>
    /// DayNightEventReceiver에서 호출됨 - 밤이 되었을 때
    /// </summary>
    public void OnNightReached()
    {
        IsNightTime = true;
        Debug.Log("<color=blue>밤이 되었습니다. 침대에서 잠을 자세요.</color>");
        OnNightStarted?.Invoke();
    }

    #endregion

    void ShowDayUI()
    {
        if (dayText != null)
        {
            dayText.text = $"Day {currentDay}";
            StartCoroutine(ShowDayTextRoutine());
        }
    }

    IEnumerator ShowDayTextRoutine()
    {
        dayText.gameObject.SetActive(true);

        // 페이드 인
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

        // 표시 유지
        yield return new WaitForSeconds(displayDuration);

        // 페이드 아웃
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

    // 하루가 지났을 때 호출되는 이벤트 (필요 시 확장)
    protected virtual void OnDayChanged()
    {
        // 여기에 하루가 지났을 때 실행할 로직 추가 가능
        // 예: 이벤트 발생, 상태 변경 등
    }

    public int GetCurrentDay()
    {
        return currentDay;
    }

    #region GUI Config

    private void SetupGui()
    {
        guiStyle = new GUIStyle();
        guiStyle.fontSize = 16;
        guiStyle.fontStyle = FontStyle.Bold;
        guiStyle.normal.textColor = Color.white;
    }

    void OnGUI()
    {
        if (showInteractMsg)
        {
            string message = IsNightTime ? "Press E/Click to Sleep" : "It's not night yet...";
            GUI.Label(new Rect(50, Screen.height - 50, 300, 50), message, guiStyle);
        }
    }

    #endregion
}
