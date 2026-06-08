using UnityEngine;

/// <summary>
/// 정지 귀신: 플레이어가 쳐다보면 멈추고, 시선을 돌리면 NavMesh를 통해 빠르게 다가옵니다.
/// </summary>
public class StalkerGhost : NavMeshGhostBase
{
    [Header("Components")]
    public Animator animator;
    private EyeBlinkController eyeController;
    private Camera mainCam;

    [Header("Detection Settings")]
    [Range(0.1f, 0.5f)]
    public float lookThreshold = 0.2f;
    public float ghostHeightOffset = 1.5f; // 귀신의 가슴/얼굴 높이 (판정 기준)
    public LayerMask obstacleLayer;        // 벽, 가구 등이 포함된 레이어

    [Header("Speed Settings")]
    [Tooltip("시선 밖에서의 속도 배율. 시선 밖 속도 = speed × 이 값.\nspeed(0.4) × 2.0 = 0.8 unit/초")]
    public float outOfSightSpeedMultiplier = 2.0f;

    [Header("Audio")]
    public AudioSource audioSource;

    private float _baseSpeed;

    protected override void Start()
    {
        base.Start(); // 부모 클래스의 타겟 설정 및 기본 초기화 수행
        _baseSpeed = speed; // 인스펙터 기본 speed 저장

        mainCam = Camera.main;
        eyeController = FindFirstObjectByType<EyeBlinkController>();

        if (animator == null) animator = GetComponent<Animator>();

        Debug.Log("<color=blue>[StalkerGhost] 정지 귀신 스폰: 시선에 반응하여 움직임을 멈춥니다.</color>");
    }

    protected override void Update()
    {
        if (isPlayerAwake || playerTarget == null) return;

        bool isBeingWatched = IsPlayerLookingAtMe();

        if (isBeingWatched)
        {
            // 시야 안 → 완전 정지
            speed = _baseSpeed;
            if (animator != null) animator.speed = 0f;
            if (agent != null) agent.isStopped = true;

            // 쳐다보고 있으면 소리 일시정지
            if (audioSource != null && audioSource.isPlaying) audioSource.Pause();
        }
        else
        {
            // 시야 밖 → 빠르게 다가옴
            speed = _baseSpeed * outOfSightSpeedMultiplier;
            if (animator != null) animator.speed = 1f;
            if (agent != null) agent.isStopped = false;

            // 시선을 돌리면 다시 소리 재생
            if (audioSource != null && !audioSource.isPlaying) audioSource.UnPause();
        }

        base.Update();
    }

    bool IsPlayerLookingAtMe()
    {
        // 0. 카메라/눈 컨트롤러는 귀신 스폰 시점에 비활성이었을 수 있으므로,
        //    악몽 카메라가 뒤늦게 활성화된 경우를 대비해 필요 시 다시 탐색합니다.
        //    (mainCam이 stale 상태면 WorldToViewportPoint 판정이 깨져 멈추지 않습니다.)
        if (mainCam == null || !mainCam.isActiveAndEnabled)
            mainCam = Camera.main;
        if (eyeController == null)
            eyeController = FindFirstObjectByType<EyeBlinkController>(FindObjectsInactive.Include);

        // 1. 눈을 감았는가?
        if (eyeController != null && eyeController.eyeOpenAmount < 0.1f) return false;
        if (mainCam == null) return false;

        // 2. 뷰포트 판정 (귀신의 특정 높이를 기준으로 화면 중앙에 있는지 확인)
        Vector3 checkPos = transform.position + Vector3.up * ghostHeightOffset;
        Vector3 screenPoint = mainCam.WorldToViewportPoint(checkPos);

        // 화면 중앙 범위 안에 들어왔는지 확인
        bool inFocusZone = screenPoint.z > 0 &&
                           screenPoint.x > (0.5f - lookThreshold) && screenPoint.x < (0.5f + lookThreshold) &&
                           screenPoint.y > (0.5f - lookThreshold) && screenPoint.y < (0.5f + lookThreshold);

        if (!inFocusZone) return false;

        // 3. 장애물(벽/가구) 감지: 플레이어와 귀신 사이에 무언가 있는지 레이캐스트
        Vector3 direction = (checkPos - mainCam.transform.position).normalized;
        float distance = Vector3.Distance(mainCam.transform.position, checkPos);

        if (Physics.Raycast(mainCam.transform.position, direction, out RaycastHit hit, distance, obstacleLayer))
        {
            // 벽이나 가구 등에 가려졌다면 "보고 있지 않음"으로 간주
            if (hit.transform != this.transform && !hit.transform.IsChildOf(this.transform))
            {
                return false;
            }
        }

        return true; // 눈을 뜨고 있고, 중앙에 있으며, 가려지지 않은 상태
    }
}