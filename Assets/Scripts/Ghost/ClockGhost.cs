using UnityEngine;
using UnityEngine.AI;
using DayNightSystem;

/// <summary>
/// 시계 귀신:
///  - 플레이어가 입력을 틀릴 때마다 이동 속도가 빨라집니다.
///  - 동시에 씬의 시계 바늘도 빠르게 돌아가 조급함을 유발합니다.
///  - 플레이어는 시계의 이상함을 인지하고 평정심을 유지해야 합니다.
/// </summary>
public class ClockGhost : NavMeshGhostBase
{
    [Header("Clock Ghost — 이동 속도")]
    [Tooltip("플레이어 실패 시 추가될 이동 속도")]
    public float failSpeedBoost = 0.8f;
    [Tooltip("귀신이 도달할 수 있는 최대 이동 속도")]
    public float maxPossibleSpeed = 15f;

    [Header("Clock Ghost — 시계 바늘 패닉")]
    [Tooltip("플레이어 실패 시 시계 바늘 속도 배율 증가량")]
    public float clockSpeedBoostPerFail = 0.8f;
    [Tooltip("시계 바늘 속도 배율 최댓값 (1 = 정상, 6 = 6배 빠름)")]
    public float maxClockSpeedMultiplier = 6f;

    [Header("Animations")]
    public Animator animator;

    private HandInputSystem[] _hands;
    private Clock _clockInScene; // MGe Labs Clock 컴포넌트

    protected override void Start()
    {
        base.Start();
        if (animator == null) animator = GetComponent<Animator>();

        _hands = FindObjectsByType<HandInputSystem>(FindObjectsSortMode.None);

        // 씬에서 Clock 컴포넌트 탐색
        _clockInScene = FindFirstObjectByType<Clock>();
        if (_clockInScene == null)
            Debug.LogWarning("[ClockGhost] 씬에서 Clock 컴포넌트를 찾지 못했습니다. 시계 바늘 효과가 적용되지 않습니다.");

        agent.speed = speed;
    }

    protected override void Update()
    {
        if (isPlayerAwake || playerTarget == null) return;

        // 플레이어 실수 실시간 감시
        foreach (var hand in _hands)
        {
            if (hand != null && hand.didJustFail)
            {
                ApplyFailurePenalty();
            }
        }

        // 애니메이션 속도 — 이동 속도가 빨라질수록 긴박하게
        if (animator != null && agent != null)
        {
            float v = agent.velocity.magnitude;
            animator.speed = (v > 0.1f) ? (v / speed) : 1f;
        }

        base.Update();
    }

    void ApplyFailurePenalty()
    {
        // 1) 이동 속도 증가
        speed = Mathf.Min(speed + failSpeedBoost, maxPossibleSpeed);
        agent.speed = speed;

        // 2) 시계 바늘 속도 증가
        if (_clockInScene != null)
        {
            _clockInScene.visualSpeedMultiplier = Mathf.Min(
                _clockInScene.visualSpeedMultiplier + clockSpeedBoostPerFail,
                maxClockSpeedMultiplier
            );
        }

        Debug.Log($"<color=red>[시계귀신] 실수 감지! " +
                  $"이동 속도: {speed:F1} / 시계 속도 배율: {_clockInScene?.visualSpeedMultiplier:F1}x</color>");
    }

    public override void OnPlayerWakeUp()
    {
        ResetClockSpeed();
        base.OnPlayerWakeUp();
    }

    private void OnDestroy()
    {
        // 귀신이 강제 제거될 때도 시계 속도 복원 보장
        ResetClockSpeed();
    }

    private void ResetClockSpeed()
    {
        if (_clockInScene != null)
            _clockInScene.ResetVisualSpeed();
    }
}
