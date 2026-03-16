using UnityEngine;

/// <summary>
/// 반사체 귀신: NavMesh를 통해 접근하며, 존재만으로 플레이어의 손 조작 시각 정보를 반전시킵니다.
/// </summary>
public class ReflectionGhost : NavMeshGhostBase
{
    [Header("Animation & Components")]
    public Animator animator;

    protected override void Start()
    {
        base.Start(); // 부모 클래스의 타겟 설정 및 기본 초기화 수행
        
        if (animator == null) animator = GetComponent<Animator>();

        SetGhostVisibility(true);

        // [특징 반영] 스폰되자마자 모든 손(왼손, 오른손)의 반전 효과 활성화
        SetMirrorEffect(true);
        
        Debug.Log("<color=purple>[ReflectionGhost] 반사체 귀신 스폰: 양손 시각 반전 활성화</color>");
    }

    protected override void Update()
    {
        if (isPlayerAwake || playerTarget == null) return;

        // 애니메이션 속도 고정
        if (animator != null) animator.speed = 1.0f;

        // 거리 기반 잡기 판정 및 기본 속도 업데이트 (NavMeshGhostBase 기능 호출)
        base.Update();
    }

    private void SetMirrorEffect(bool active)
    {
        HandInputSystem[] allHands = FindObjectsByType<HandInputSystem>(FindObjectsSortMode.None);
        foreach (var hand in allHands)
        {
            if (hand != null) hand.isVisualMirrored = active;
        }
    }

    void SetGhostVisibility(bool visible)
    {
        Renderer[] rs = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in rs) r.enabled = visible;
    }

    public override void OnPlayerWakeUp() 
    { 
        isPlayerAwake = true; 
        // 효과 해제
        SetMirrorEffect(false);
        base.OnPlayerWakeUp(); 
    }

    private void OnDestroy()
    {
        // 귀신이 제거될 때 효과가 남지 않도록 보장
        SetMirrorEffect(false);
    }
}