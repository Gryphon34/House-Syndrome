using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

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

    public Slider individualGaugeUI;

    // ��ġ �������� ���� DifficultyManager���� �����ɴϴ�.
    private float cycleIncreaseAmount;
    private float failPenaltyAmount;
    private float constantDecayRate;
    private int sequenceLength;

    [Header("Visibility Settings (Raycast)")]
    public LayerMask handLayer;
    public float rayDistance = 10f;

    private Quaternion initialThumbRotation;
    private Quaternion[] initialFingerRotations;
    private List<KeyCode> currentSequence = new List<KeyCode>();
    private int currentIndex = 0;
    private Camera mainCam;

    private EyeBlinkController eyeController;
    void Start()
    {
        // [�߰�] ���� ���۵ǰų� ��ε�� �� static �������� 0���� �����մϴ�.
        leftGauge = 0f;
        rightGauge = 0f;

        // DifficultyManager�� �ִ��� Ȯ���ϰ� ��ġ ��������
        UpdateDifficultyFromManager();

        mainCam = Camera.main;

        eyeController=FindFirstObjectByType<EyeBlinkController>();
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
        currentIndex++;
        if (currentIndex >= currentSequence.Count)
        {
            if (handSide == HandSide.Left)
                leftGauge = Mathf.Min(maxGaugePerHand, leftGauge + cycleIncreaseAmount);
            else
                rightGauge = Mathf.Min(maxGaugePerHand, rightGauge + cycleIncreaseAmount);

            StartCoroutine(ShowCycleFeedback());
            GenerateNewSequence();
        }
    }

    void FailInput()
    {
        if (handSide == HandSide.Left)
            leftGauge = Mathf.Max(0, leftGauge - failPenaltyAmount);
        else
            rightGauge = Mathf.Max(0, rightGauge - failPenaltyAmount);

        GenerateNewSequence();
    }

    void GenerateNewSequence()
    {
        currentSequence.Clear();
        currentIndex = 0;
        // �Ŵ������� �޾ƿ� ���̸� ����մϴ�.
        for (int i = 0; i < sequenceLength; i++)
            currentSequence.Add(fingerKeys[Random.Range(0, fingerKeys.Length)]);
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
        if (Input.GetKey(thumbKey))
        {
            RotateBone(thumbBone, initialThumbRotation, bendAngle);
            for (int i = 0; i < fingerKeys.Length; i++)
            {
                if (Input.GetKeyDown(fingerKeys[i]))
                {
                    StartCoroutine(FingerTapRoutine(i));
                    if (fingerKeys[i] == currentSequence[currentIndex]) SuccessInput();
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
    IEnumerator FingerTapRoutine(int index)
    {
        if (index >= fingerBones.Length || fingerBones[index] == null) yield break;
        RotateBone(fingerBones[index], initialFingerRotations[index], bendAngle);
        yield return new WaitForSeconds(0.1f);
        fingerBones[index].localRotation = initialFingerRotations[index];
    }

    void RotateBone(Transform bone, Quaternion baseRot, float angle)
    {
        if (bone != null) bone.localRotation = baseRot * Quaternion.Euler(rotationAxis * angle);
    }

    void SetupUI()
    {
        if (thumbUI != null) thumbUI.GetComponent<TextMeshProUGUI>().text = thumbKey.ToString();
        for (int i = 0; i < fingerUIs.Length; i++)
        {
            if (fingerUIs[i] != null) fingerUIs[i].GetComponent<TextMeshProUGUI>().text = fingerKeys[i].ToString();
        }
    }

    void UpdateUIPositions()
    {
        if (mainCam == null || !uiParentGroup.activeSelf) return;
        FollowTarget(thumbUITarget, thumbUI);
        for (int i = 0; i < fingerUITargets.Length; i++)
        {
            if (i >= fingerUIs.Length || fingerUIs[i] == null || fingerUITargets[i] == null) continue;
            FollowTarget(fingerUITargets[i], fingerUIs[i]);
            var t = fingerUIs[i].GetComponent<TextMeshProUGUI>();
            if (t != null && currentSequence.Count > currentIndex)
            {
                t.color = (fingerKeys[i] == currentSequence[currentIndex]) ? targetColor : normalColor;
            }
        }
    }

    void FollowTarget(Transform target, RectTransform ui)
    {
        if (target == null || ui == null) return;
        Vector3 screenPos = mainCam.WorldToScreenPoint(target.position);
        if (screenPos.z > 0)
        {
            ui.gameObject.SetActive(true);
            ui.position = screenPos;
        }
        else ui.gameObject.SetActive(false);
    }
}