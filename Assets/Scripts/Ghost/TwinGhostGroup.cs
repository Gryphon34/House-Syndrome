using UnityEngine;

public class TwinGhostGroup : NavMeshGhostBase
{
    private TwinGhost[] twins;

    protected override void Awake()
    {
        // 부모는 직접 움직이지 않으므로 에이전트 비활성화
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        twins = GetComponentsInChildren<TwinGhost>();
    }

    public override void SetTarget(Transform target)
    {
        // 모든 자식 쌍둥이에게 타겟 전달
        foreach (var twin in twins)
        {
            twin.SetTarget(target);
        }
    }

    public override void OnPlayerWakeUp()
    {
        foreach (var twin in twins)
        {
            twin.OnPlayerWakeUp();
        }
        base.OnPlayerWakeUp(); // 부모 오브젝트 제거
    }
}