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
    public float cycleIncreaseAmount = 12f; // 사이클 완성 시 증가량 (상시 감소를 고려해 조금 상향)
    public float failPenaltyAmount = 5f;    // 오입력 시 즉시 차감

    [Space(10)]
    public float constantDecayRate = 1.0f;  // [추가] 조작 중에도 발생하는 상시 감소량
    public float individualWinThreshold = 80f;

    public Slider individualGaugeUI;

    [Header("Visibility Settings (Raycast)")]
    public LayerMask handLayer;
    public float rayDistance = 10f;

    private Quaternion initialThumbRotation;
    private Quaternion[] initialFingerRotations;
    private List<KeyCode> currentSequence = new List<KeyCode>();
    private int currentIndex = 0;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        if (thumbBone != null) initialThumbRotation = thumbBone.localRotation;

        initialFingerRotations = new Quaternion[fingerBones.Length];
        for (int i = 0; i < fingerBones.Length; i++)
        {
            if (fingerBones[i] != null) initialFingerRotations[i] = fingerBones[i].localRotation;
        }

        GenerateNewSequence();
        SetupUI();
    }

    void LateUpdate()
    {
        UpdateVisibilityByRaycast();
        UpdateUIPositions();
    }

    void Update()
    {
        CheckInput();
        ApplyGaugeDecay(); // 강화된 감소 로직 실행
        UpdateGaugeUI();
        CheckWinCondition();
    }

    // [로직 강화] 상시 감소 + 방치 시 가속 감소
    void ApplyGaugeDecay()
    {
        float currentDecay = constantDecayRate; // 기본적으로 항상 감소함

        if (handSide == HandSide.Left)
            leftGauge = Mathf.Max(0, leftGauge - currentDecay * Time.deltaTime);
        else
            rightGauge = Mathf.Max(0, rightGauge - currentDecay * Time.deltaTime);
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
        Debug.Log($"<color=red>{handSide} 입력 실패! 시퀀스 및 게이지 패널티.</color>");

        if (handSide == HandSide.Left)
            leftGauge = Mathf.Max(0, leftGauge - failPenaltyAmount);
        else
            rightGauge = Mathf.Max(0, rightGauge - failPenaltyAmount);

        GenerateNewSequence();
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

    void WakeUp()
    {
        Debug.Log("<color=yellow>가위 탈출 성공!</color>");
    }

    void UpdateVisibilityByRaycast()
    {
        if (mainCam == null || uiParentGroup == null) return;
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        bool isLookingAtMe = false;

        if (Physics.Raycast(ray, out hit, rayDistance, handLayer))
        {
            Transform handRoot = thumbBone.parent.parent;
            if (hit.transform == handRoot || hit.transform.IsChildOf(handRoot))
            {
                isLookingAtMe = true;
            }
        }
        uiParentGroup.SetActive(isLookingAtMe);
    }

    // ... (이하 유틸리티 함수 FingerTapRoutine, RotateBone, SetupUI, UpdateUIPositions, FollowTarget, GenerateNewSequence 동일)
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

    void GenerateNewSequence()
    {
        currentSequence.Clear();
        currentIndex = 0;
        for (int i = 0; i < 4; i++)
            currentSequence.Add(fingerKeys[Random.Range(0, fingerKeys.Length)]);
    }
}