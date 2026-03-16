using UnityEngine;

/// <summary>
/// 일반 귀신: 특수 능력 없이 NavMesh를 따라 정직하게 플레이어에게 다가옵니다.
/// </summary>
public class NormalGhost : NavMeshGhostBase
{
    [Header("Animations")]
    public Animator animator;

    protected override void Start()
    {
        base.Start();
        if (animator == null) animator = GetComponent<Animator>();

        SetGhostVisibility(true);
        Debug.Log("<color=green>[NormalGhost] 일반 귀신 스폰: 애니메이션 활성화</color>");
    }

    protected override void Update()
    {
        base.Update(); // 기본 이동 및 거리 체크 수행

        // [추가] 애니메이션 속도를 실제 이동 속도와 동기화
        if (animator != null && agent != null)
        {
            // 현재 속도가 0보다 크면 애니메이션 재생, 아니면 정지
            float velocityMagnitude = agent.velocity.magnitude;
            animator.speed = (velocityMagnitude > 0.1f) ? (velocityMagnitude / speed) : 1f;
            
            // 만약 Animator에 'Speed' 파라미터가 있다면 아래 주석을 해제하세요.
            // animator.SetFloat("Speed", velocityMagnitude);
        }
    }

    private void SetGhostVisibility(bool visible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = visible;
    }
}