using UnityEngine;
using UnityEngine.AI;

public class ClockGhost : NavMeshGhostBase
{
    [Header("Clock Ghost Specials")]
    [Tooltip("플레이어 실패 시 추가될 속도 수치")]
    public float failSpeedBoost = 0.8f;   
    [Tooltip("귀신이 도달할 수 있는 최대 속도")]
    public float maxPossibleSpeed = 15f;  

    [Header("Animations")]
    public Animator animator;

    private HandInputSystem[] hands;

    protected override void Start()
    {
        base.Start();
        if (animator == null) animator = GetComponent<Animator>();

        hands = FindObjectsByType<HandInputSystem>(FindObjectsSortMode.None);
        agent.speed = speed;
    }

    protected override void Update()
    {
        if (isPlayerAwake || playerTarget == null) return;

        // 1. 플레이어의 실수 실시간 감시
        foreach (var hand in hands)
        {
            if (hand.didJustFail)
            {
                ApplyFailurePenalty();
            }
        }

        // 2. [추가] 애니메이션 속도 제어
        if (animator != null && agent != null)
        {
            float velocityMagnitude = agent.velocity.magnitude;
            // 속도가 빨라질수록 애니메이션도 빠르게 재생 (최소 1배속 유지)
            animator.speed = (velocityMagnitude > 0.1f) ? (velocityMagnitude / speed) : 1f;
        }

        // 3. 기본 이동 및 잡기 판정 수행 (NavMeshGhostBase 기능)
        base.Update();
    }

    void ApplyFailurePenalty()
    {
        // 실패 시 속도 증가
        speed = Mathf.Min(speed + failSpeedBoost, maxPossibleSpeed);
        agent.speed = speed;
        
        Debug.Log($"<color=red>[시계귀신] 플레이어 실수! 추격 속도 증가: {speed}</color>");
    }
}