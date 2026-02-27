using UnityEngine;

public class StalkerGhost : MonoBehaviour
{
    [Header("Movement Settings")]
    public Transform playerTarget;
    public float timeToReach = 15f;
    public float catchDistance = 0.5f;

    [Header("Components")]
    public Animator animator;
    private EyeBlinkController eyeController;
    private Camera mainCam;

    [Header("Detection Settings")]
    [Range(0.1f, 0.5f)]
    public float lookThreshold = 0.2f;
    public float ghostHeightOffset = 1.5f; // 귀신의 가슴/얼굴 높이 (판정 기준)
    public LayerMask obstacleLayer;        // 벽, 가구 등이 포함된 레이어

    private Transform[] waypoints;
    private float timer = 0f;
    private Vector3 lastPosition;
    private bool isPlayerAwake = false;

    void Start()
    {
        lastPosition = transform.position;
        mainCam = Camera.main;
        eyeController = FindFirstObjectByType<EyeBlinkController>();

        if (playerTarget == null)
        {
            GameObject playerObj = GameObject.Find("NightMarePlayer");
            if (playerObj != null) playerTarget = playerObj.transform;
        }

        if (animator == null) animator = GetComponent<Animator>();
    }

    public void SetTarget(Transform target) { playerTarget = target; }
    public void SetPath(Transform[] path) { waypoints = path; }

    void Update()
    {
        if (isPlayerAwake || playerTarget == null) return;

        // [핵심] 쳐다보고 있는지 + 벽에 가려졌는지 체크
        bool isBeingWatched = IsPlayerLookingAtMe();

        if (isBeingWatched)
        {
            if (animator != null) animator.speed = 0f;
        }
        else
        {
            if (animator != null) animator.speed = 1f;
            timer += Time.deltaTime;

            // 이동 로직 (기존과 동일)
            float segmentTime = timeToReach / (waypoints != null && waypoints.Length > 0 ? waypoints.Length : 1);
            if (waypoints == null || waypoints.Length == 0)
                transform.position = Vector3.Lerp(lastPosition, playerTarget.position, timer / timeToReach);
            else
                MoveAlongPath(segmentTime);
        }

        float currentDistance = Vector3.Distance(transform.position, playerTarget.position);
        if (currentDistance <= catchDistance) CatchPlayer();
    }

    bool IsPlayerLookingAtMe()
    {
        // 1. 눈을 감았는가?
        if (eyeController != null && eyeController.eyeOpenAmount < 0.1f) return false;
        if (mainCam == null) return false;

        // 2. 뷰포트 판정 (가슴 높이 기준)
        Vector3 checkPos = transform.position + Vector3.up * ghostHeightOffset;
        Vector3 screenPoint = mainCam.WorldToViewportPoint(checkPos);

        // 화면 중앙 범위 안에 들어왔는지 확인
        bool inFocusZone = screenPoint.z > 0 &&
                           screenPoint.x > (0.5f - lookThreshold) && screenPoint.x < (0.5f + lookThreshold) &&
                           screenPoint.y > (0.5f - lookThreshold) && screenPoint.y < (0.5f + lookThreshold);

        if (!inFocusZone) return false;

        // 3. [추가] 장애물(벽/가구) 감지
        // 카메라에서 귀신의 가슴 높이 지점까지 레이를 쏩니다.
        Vector3 direction = (checkPos - mainCam.transform.position).normalized;
        float distance = Vector3.Distance(mainCam.transform.position, checkPos);

        // 레이캐스트가 장애물 레이어에 부딪히면 "가려진 것"으로 판단하여 움직이게 합니다.
        if (Physics.Raycast(mainCam.transform.position, direction, out RaycastHit hit, distance, obstacleLayer))
        {
            // 만약 부딪힌 게 귀신 본인이 아니라면(벽이라면) false 반환
            if (hit.transform != this.transform && !hit.transform.IsChildOf(this.transform))
            {
                return false;
            }
        }

        return true; // 눈 뜨고 + 중앙에 있고 + 가려지지 않음 = 멈춤
    }

    // ... (MoveAlongPath, CatchPlayer 등 나머지 함수는 동일)
    void MoveAlongPath(float segmentTime)
    {
        float progress = (timer % segmentTime) / segmentTime;
        int index = Mathf.FloorToInt(timer / segmentTime);
        if (index < waypoints.Length)
        {
            Vector3 start = (index == 0) ? lastPosition : waypoints[index - 1].position;
            Vector3 end = waypoints[index].position;
            transform.position = Vector3.Lerp(start, end, progress);
        }
        else
        {
            transform.position = Vector3.Lerp(waypoints[waypoints.Length - 1].position, playerTarget.position, (timer - timeToReach) / 2f);
        }
    }

    void CatchPlayer()
    {
        if (isPlayerAwake) return;
        if (SpawnManager.Instance != null) SpawnManager.Instance.ReturnToPreviousDay();
    }
}