using UnityEngine;

public class TwinGhost : NavMeshGhostBase
{
    [Header("Twin Settings")]
    public float targetArrivalTime = 20f; // 도달 목표 시간 (초)
    public float amplitude = 0.5f;        // 속도 변화 폭
    public float frequency = 2f;          // 속도 변화 주기
    public bool isGhostA = true;          // A는 빨랐다 느려짐, B는 느렸다 빨라짐

    [Header("Animations")]
    public Animator animator;

    private float elapsedTime = 0f;

    protected override void Start()
    {
        base.Start();
        if (animator == null) animator = GetComponent<Animator>();
        
        // 쌍둥이 등장 시 입력 시스템 모드 전환
        SetTwinInputMode(true);
    }

    protected override void Update()
    {
        if (isPlayerAwake || playerTarget == null) return;

        elapsedTime += Time.deltaTime;
        float remainingTime = Mathf.Max(targetArrivalTime - elapsedTime, 0.1f);
        float distance = Vector3.Distance(transform.position, playerTarget.position);

        // [핵심] 남은 거리를 남은 시간으로 나누고, 사인파를 곱해 속도 교차 발생
        float baseSpeed = distance / remainingTime;
        float oscillation = Mathf.Sin(elapsedTime * frequency) * amplitude;
        
        speed = baseSpeed * (isGhostA ? (1f + oscillation) : (1f - oscillation));
        
        if (agent != null) agent.speed = speed;

        // [추가] 애니메이션 속도를 실제 이동 속도와 동기화
        if (animator != null && agent != null)
        {
            float velocityMagnitude = agent.velocity.magnitude;
            // 속도가 빨라질 때 애니메이션도 긴박하게 재생됨 (기준 속도 1.5로 나눔)
            animator.speed = (velocityMagnitude > 0.1f) ? (velocityMagnitude / 1.5f) : 1f;
        }

        base.Update(); // 거리 기반 잡기 판정 실행
    }

    private void SetTwinInputMode(bool active)
    {
        HandInputSystem[] hands = FindObjectsByType<HandInputSystem>(FindObjectsSortMode.None);
        foreach (var hand in hands) 
        {
            // [수정 1] 주석을 해제하여 실제로 모드를 변경합니다.
            hand.isTwinMode = active; 
            
            // [수정 2] 모드가 변경되었으므로 즉시 새로운 시퀀스(두 번 입력형)를 생성합니다.
            hand.GenerateNewSequence(); 
            
            Debug.Log($"<color=yellow>[TwinGhost] {hand.handSide} 입력 모드 전환: TwinMode = {active}</color>");
        }
    }

    public override void OnPlayerWakeUp()
    {
        SetTwinInputMode(false);
        base.OnPlayerWakeUp();
    }
}