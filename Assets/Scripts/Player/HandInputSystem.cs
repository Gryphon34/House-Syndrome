using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class HandInputSystem : MonoBehaviour
{
    public enum HandSide { Left, Right }
    public HandSide handSide;

    [Header("Input Settings")]
    public KeyCode thumbKey;
    public KeyCode[] fingerKeys;

    [Header("Bone Settings (Movement)")]
    public Transform thumbBone;
    public Transform[] fingerBones;
    public Vector3 rotationAxis = new Vector3(1, 0, 0);
    public float bendAngle = 30f;

    [Header("UI Target Settings (Anchor)")]
    public Transform thumbUITarget;
    public Transform[] fingerUITargets;

    [Header("UI Elements")]
    public GameObject uiParentGroup;
    public RectTransform thumbUI;
    public RectTransform[] fingerUIs;
    public Color normalColor = Color.white;
    public Color targetColor = Color.yellow;
    public TextMeshProUGUI cycleFeedbackText;

    [Header("Game Logic - Gauge")]
    public static float leftGauge = 0f;
    public static float rightGauge = 0f;
    public float maxGaugePerHand = 100f;
    public float individualWinThreshold = 80f;

    [Header("Reflector Ghost Effect")]
    public bool isVisualMirrored = false; // [핵심] 반사체 귀신이 이 값을 켭니다.

    public Slider individualGaugeUI;

    private float cycleIncreaseAmount;
    private float failPenaltyAmount;
    private float constantDecayRate;
    private int sequenceLength;

    [Header("Visibility Settings (Raycast)")]
    public LayerMask handLayer;
    public float rayDistance = 10f;

    [Header("Reflector Ghost Keys")]
    public KeyCode mirroredThumbKey;//반전 시 엄지 키
    public KeyCode[] mirroredFingerKeys;// 반전 시 각 손가락 키

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip correctKeyClip;    // 개별 키 성공
    public AudioClip cycleSuccessClip; // 사이클 완료
    public AudioClip failClip; //입력 실패

    private Quaternion initialThumbRotation;
    private Quaternion[] initialFingerRotations;
    private List<KeyCode> currentSequence = new List<KeyCode>();
    private int currentIndex = 0;
    private Camera mainCam;

    private EyeBlinkController eyeController;

    public bool didJustFail = false; // 이번 프레임에 실패했는지 여부
    public bool didJustSucceed = false; // 이번 사이클을 성공했는지 여부

    public bool isTwinMode = false; // 쌍둥이 귀신 등장 시 true로 설정

    void Start()
    {
        leftGauge = 0f;
        rightGauge = 0f;

        UpdateDifficultyFromManager();
        mainCam = Camera.main;
        eyeController = FindFirstObjectByType<EyeBlinkController>();

        if (thumbBone != null) initialThumbRotation = thumbBone.localRotation;
        initialFingerRotations = new Quaternion[fingerBones.Length];
        for (int i = 0; i < fingerBones.Length; i++)
        {
            if (fingerBones[i] != null) initialFingerRotations[i] = fingerBones[i].localRotation;
        }

        GenerateNewSequence();
        SetupUI();
    }

    void UpdateDifficultyFromManager()
    {
        if (DifficultyManager.Instance != null)
        {
            constantDecayRate = DifficultyManager.Instance.GetConstantDecayRate();
            cycleIncreaseAmount = DifficultyManager.Instance.GetCycleIncreaseAmount();
            failPenaltyAmount = DifficultyManager.Instance.GetFailPenaltyAmount();
            sequenceLength = DifficultyManager.Instance.GetSequenceLength();
        }
        else
        {
            // �Ŵ����� ���� ��츦 ����� �⺻��
            constantDecayRate = 1.0f;
            cycleIncreaseAmount = 10f;
            failPenaltyAmount = 5f;
            sequenceLength = 4;
        }
    }

    void Update()
    {
        CheckInput();
        ApplyGaugeDecay();
        UpdateGaugeUI();
        CheckWinCondition();
    }

    void ApplyGaugeDecay()
    {
        float decayMultiplier = 1.0f;
        // �� ���ų� ���� ���� �� ���� ���� (��¥�� ���� �� �������� �Ŵ��� ���� ����)
        if (!uiParentGroup.activeSelf || !Input.GetKey(thumbKey))
        {
            decayMultiplier = 3.0f;
        }

        float totalDecay = constantDecayRate * decayMultiplier;

        if (handSide == HandSide.Left)
            leftGauge = Mathf.Max(0, leftGauge - totalDecay * Time.deltaTime);
        else
            rightGauge = Mathf.Max(0, rightGauge - totalDecay * Time.deltaTime);
    }

    void SuccessInput()
{
    // [추가] 개별 키 입력 성공 사운드 재생
    if (audioSource != null && correctKeyClip != null)
        audioSource.PlayOneShot(correctKeyClip);

    currentIndex++;
    if (currentIndex >= currentSequence.Count)
    {
        // [추가] 한 사이클 전체 성공 사운드 재생
        if (audioSource != null && cycleSuccessClip != null)
            audioSource.PlayOneShot(cycleSuccessClip);

        didJustSucceed = true; 
        float accelerationMultiplier = 1f + ( (leftGauge + rightGauge) / (maxGaugePerHand * 2f) );
        float finalGain = cycleIncreaseAmount * accelerationMultiplier;

        if (handSide == HandSide.Left)
            leftGauge = Mathf.Min(maxGaugePerHand, leftGauge + finalGain);
        else
            rightGauge = Mathf.Min(maxGaugePerHand, rightGauge + finalGain);

        StartCoroutine(ResetSuccessFlag());
        GenerateNewSequence();
    }
}
    void FailInput()
{
    // [추가] 입력 실패 사운드 재생
    if (audioSource != null && failClip != null)
        audioSource.PlayOneShot(failClip);

    didJustFail = true; 
    StartCoroutine(ResetFailFlag());

    float heavyPenalty = failPenaltyAmount * 3f; 
    if (handSide == HandSide.Left)
        leftGauge = Mathf.Max(0, leftGauge - heavyPenalty);
    else
        rightGauge = Mathf.Max(0, rightGauge - heavyPenalty);

    GenerateNewSequence();
}

    IEnumerator ResetFailFlag() { yield return new WaitForEndOfFrame(); didJustFail = false; }
    IEnumerator ResetSuccessFlag() { yield return new WaitForEndOfFrame(); didJustSucceed = false; }

    public void GenerateNewSequence()
{
    currentSequence.Clear();
    currentIndex = 0;

    // [핵심 수정] 현재 반사체 효과 유무에 따라 시퀀스 생성에 사용할 키 풀을 선택합니다.
    KeyCode[] activePool = isVisualMirrored ? mirroredFingerKeys : fingerKeys;

    // 에러 방지: 키 풀이 비어있으면 중단
    if (activePool == null || activePool.Length == 0) return;

    for (int i = 0; i < sequenceLength; i++)
    {
        // 이제 activePool(반전 시에는 mirroredFingerKeys)에서 키를 뽑습니다.
        KeyCode targetKey = activePool[Random.Range(0, activePool.Length)];
        
        if (isTwinMode)
        {
            currentSequence.Add(targetKey);
            currentSequence.Add(targetKey);
        }
        else
        {
            currentSequence.Add(targetKey);
        }
    }
}

    // HandInputSystem.cs�� WakeUp �Լ� ����

    void WakeUp()
    {
        Debug.Log("<color=cyan>���� Ż�� ����!</color>");

        // 1. ������ �ʱ�ȭ
        leftGauge = 0;
        rightGauge = 0;

        // 2. SpawnManager�� ���� ���� ȣ��
        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.AdvanceDayFromNightmare();
        }
    }

    void LateUpdate()
    {
        UpdateVisibilityByRaycast();
        UpdateUIPositions();
    }

    void OnEnable()
    {
        // ������Ʈ�� ���� �� ���� Ȱ��ȭ�� ���� ī�޶�(NightmareCamera)�� �ٽ� �����ɴϴ�.
        mainCam = Camera.main;
        UpdateDifficultyFromManager();
    }


    void CheckInput()
{
    // [핵심] 현재 귀신 효과 유무에 따라 사용할 키 세트를 결정 (왼손/오른손 개별 적용)
    KeyCode activeThumb = isVisualMirrored ? mirroredThumbKey : thumbKey;
    KeyCode[] activeFingers = isVisualMirrored ? mirroredFingerKeys : fingerKeys;

    if (Input.GetKey(activeThumb))
    {
        RotateBone(thumbBone, initialThumbRotation, bendAngle);
        for (int i = 0; i < activeFingers.Length; i++)
        {
            if (Input.GetKeyDown(activeFingers[i]))
            {
                // 손가락 애니메이션 반전 (시각적 기괴함 유지)
                int visualIndex = isVisualMirrored ? (activeFingers.Length - 1 - i) : i;
                StartCoroutine(FingerTapRoutine(visualIndex));

                // 현재 활성화된 키(activeFingers)로 정직하게 입력 판정
                if (activeFingers[i] == currentSequence[currentIndex]) SuccessInput();
                else FailInput();
            }
        }
    }
    else if (thumbBone != null) thumbBone.localRotation = initialThumbRotation;
}


    IEnumerator ShowCycleFeedback()
    {
        if (cycleFeedbackText != null)
        {
            cycleFeedbackText.text = "CYCLE COMPLETE!";
            cycleFeedbackText.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.6f);
            cycleFeedbackText.gameObject.SetActive(false);
        }
    }

    void UpdateGaugeUI()
    {
        if (individualGaugeUI != null)
        {
            individualGaugeUI.value = (handSide == HandSide.Left) ? leftGauge : rightGauge;
        }
    }

    void CheckWinCondition()
    {
        if (leftGauge >= individualWinThreshold && rightGauge >= individualWinThreshold)
        {
            WakeUp();
        }
    }

    void UpdateVisibilityByRaycast()
    {
        if (mainCam == null || !mainCam.gameObject.activeInHierarchy)
        {
            mainCam = Camera.main;
        }

        if (mainCam == null || uiParentGroup == null) return;

        // [����] ���� ���� ���� ���¶�� UI�� �ƿ� ǥ������ ����
        if (eyeController != null && eyeController.eyeOpenAmount < 0.1f)
        {
            uiParentGroup.SetActive(false);
            return;
        }

        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        bool isLookingAtMe = false;

        // ���� ����ĳ��Ʈ ���� ����
        if (Physics.Raycast(ray, out hit, rayDistance, handLayer))
        {
            string hitName = hit.transform.name;
            if (handSide == HandSide.Left && hitName.Contains("Left")) isLookingAtMe = true;
            else if (handSide == HandSide.Right && hitName.Contains("Right")) isLookingAtMe = true;
        }

        uiParentGroup.SetActive(isLookingAtMe);
    }

    // ... (���� ��ƿ��Ƽ �Լ� FingerTapRoutine, RotateBone, SetupUI, UpdateUIPositions, FollowTarget, GenerateNewSequence ����)
    IEnumerator FingerTapRoutine(int index) {
        if (index >= fingerBones.Length || fingerBones[index] == null) yield break;
        RotateBone(fingerBones[index], initialFingerRotations[index], bendAngle);
        yield return new WaitForSeconds(0.1f);
        fingerBones[index].localRotation = initialFingerRotations[index];
    }

    void RotateBone(Transform bone, Quaternion baseRot, float angle) {
        if (bone != null) bone.localRotation = baseRot * Quaternion.Euler(rotationAxis * angle);
    }

    void SetupUI() {
        if (thumbUI != null) thumbUI.GetComponent<TextMeshProUGUI>().text = thumbKey.ToString();
        for (int i = 0; i < fingerUIs.Length; i++) {
            if (fingerUIs[i] != null) fingerUIs[i].GetComponent<TextMeshProUGUI>().text = fingerKeys[i].ToString();
        }
    }

    void UpdateUIPositions()
{
    if (mainCam == null || !uiParentGroup.activeSelf) return;

    FollowTarget(thumbUITarget, thumbUI);

    // 현재 모드에 맞는 키 세트 참조
    KeyCode activeThumb = isVisualMirrored ? mirroredThumbKey : thumbKey;
    KeyCode[] activeFingers = isVisualMirrored ? mirroredFingerKeys : fingerKeys;

    // 엄지 UI 텍스트 업데이트
    var thumbText = thumbUI.GetComponent<TextMeshProUGUI>();
    if (thumbText != null) thumbText.text = activeThumb.ToString();

    int targetKeyIndex = -1;
    if (currentSequence.Count > currentIndex)
    {
        targetKeyIndex = System.Array.IndexOf(activeFingers, currentSequence[currentIndex]);
    }

    for (int i = 0; i < fingerUITargets.Length; i++)
    {
        if (i >= fingerUIs.Length || fingerUIs[i] == null || fingerUITargets[i] == null) continue;
        FollowTarget(fingerUITargets[i], fingerUIs[i]);
        
        var t = fingerUIs[i].GetComponent<TextMeshProUGUI>();
        if (t != null)
        {
            // UI에 현재 눌러야 할 실제 키(반전된 키 포함)를 표시
            t.text = activeFingers[i].ToString();
            t.color = (i == targetKeyIndex) ? targetColor : normalColor;
        }
    }
}

    void FollowTarget(Transform target, RectTransform ui) {
        if (target == null || ui == null) return;
        Vector3 screenPos = mainCam.WorldToScreenPoint(target.position);
        if (screenPos.z > 0) { ui.gameObject.SetActive(true); ui.position = screenPos; }
        else ui.gameObject.SetActive(false);
    }
}