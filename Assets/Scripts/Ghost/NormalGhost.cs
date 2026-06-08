using UnityEngine;

/// <summary>
/// 일반 귀신: 플레이어를 향해 직선 추적합니다.
/// 대응법: 눈 감기(eyeOpenAmount < 0.1) 또는 시야 피하기(장애물로 LoS 차단).
/// 두 조건 중 하나라도 충족되면 귀신이 멈춥니다.
/// </summary>
public class NormalGhost : NavMeshGhostBase
{
    [Header("Animations")]
    public Animator animator;

    [Header("Detection — 눈 감기 / 시야 피하기")]
    [Tooltip("플레이어와 귀신 사이의 장애물 레이어 (벽, 가구 등). 장애물에 가리면 추적 중단.")]
    public LayerMask obstacleLayer;
    [Tooltip("귀신이 플레이어를 감지할 수 있는 최대 거리")]
    public float sightRange = 20f;
    [Tooltip("레이캐스트 시작점 높이 오프셋 (귀신 몸 기준)")]
    public float castHeightOffset = 1f;

    private EyeBlinkController _eyeController;

    protected override void Start()
    {
        base.Start();
        if (animator == null) animator = GetComponent<Animator>();

        // includeInactive: true → WalkingPlayer 등 비활성 오브젝트에 붙은 컴포넌트도 탐색
        _eyeController = FindObjectOfType<EyeBlinkController>(true);

        Debug.Log("<color=green>[NormalGhost] 일반 귀신 스폰: 눈 감기·시야 피하기 대응 활성화</color>");
    }

    protected override void Update()
    {
        if (isPlayerAwake || playerTarget == null) return;

        // Start()에서 못 찾은 경우 매 프레임 재시도 (씬 구성에 따른 안전장치)
        if (_eyeController == null)
            _eyeController = FindObjectOfType<EyeBlinkController>(true);

        bool canChase = CanSeePlayer();

        // 귀신이 플레이어를 인식하지 못하면 멈춤
        agent.isStopped = !canChase;

        // 애니메이션 속도를 실제 이동 속도와 동기화
        if (animator != null && agent != null)
        {
            float v = agent.velocity.magnitude;
            animator.speed = (v > 0.1f) ? (v / speed) : 1f;
        }

        // 기본 이동(SetDestination) · 거리 잡기 판정 · 주기 사운드 처리
        base.Update();
    }

    /// <summary>
    /// 귀신이 플레이어를 인식할 수 있는지 판단.
    /// 1) 플레이어가 눈을 감았으면 → 인식 불가
    /// 2) 사거리 초과 → 인식 불가
    /// 3) 귀신 → 플레이어 방향 장애물에 가려지면 → 인식 불가
    /// </summary>
    bool CanSeePlayer()
    {
        // 1. 눈을 감고 있으면 귀신 인식 차단 (눈 감기 대응법)
        if (_eyeController != null && _eyeController.eyeOpenAmount < 0.1f)
            return false;

        if (playerTarget == null) return false;

        float dist = Vector3.Distance(transform.position, playerTarget.position);

        // 2. 최대 사거리 초과
        if (dist > sightRange)
            return false;

        // 3. 귀신 → 플레이어 방향 장애물 레이캐스트 (시야 피하기 대응법)
        Vector3 origin    = transform.position + Vector3.up * castHeightOffset;
        Vector3 targetPos = playerTarget.position + Vector3.up * castHeightOffset;
        Vector3 direction = (targetPos - origin).normalized;

        if (Physics.Raycast(origin, direction, dist, obstacleLayer))
            return false; // 장애물에 가림 → 추적 중단

        return true;
    }
}
