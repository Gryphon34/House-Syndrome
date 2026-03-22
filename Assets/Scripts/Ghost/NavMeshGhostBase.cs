using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public abstract class NavMeshGhostBase : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 1.0f; 
    public float catchDistance = 1.5f;

    protected NavMeshAgent agent;
    protected Transform playerTarget;
    protected bool isPlayerAwake = false;

    [Header("Jump Scare Setting")]
    // [추가] 각 귀신 프리팹에서 어떤 귀신인지 인스펙터에서 설정합니다.
    public GhostType myGhostType;
    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    // [수정] 자식 클래스에서 override 할 수 있도록 virtual Start 추가
    protected virtual void Start() 
    {
        // 기본적으로 타겟을 향해 이동 시작
        if (playerTarget != null && agent != null)
        {
            agent.SetDestination(playerTarget.position);
        }
    }

    public virtual void SetTarget(Transform target)
    {
        playerTarget = target;
    }

    protected virtual void Update()
    {
        if (isPlayerAwake || playerTarget == null) return;

        agent.speed = speed;

        if (Vector3.Distance(transform.position, playerTarget.position) <= catchDistance)
        {
            CatchPlayer();
        }
    }

    protected void CatchPlayer()
{
    if (isPlayerAwake) return;

    // 모든 손 입력 중지
    HandInputSystem[] allHands = FindObjectsByType<HandInputSystem>(FindObjectsSortMode.None);
    foreach (var hand in allHands) hand.StopAllCoroutines();

    // 이미지 방식 매니저 호출
    if (JumpScareManager.Instance != null)
    {
        JumpScareManager.Instance.TriggerJumpScare(myGhostType);
    }
    else
    {
        if (SpawnManager.Instance != null) SpawnManager.Instance.ReturnToPreviousDay();
    }
}

    public virtual void OnPlayerWakeUp()
    {
    // 자식 클래스에서 별도로 정의하지 않았을 때 실행될 기본 로직입니다.
    // 가위눌림에서 깨어났을 때 귀신 오브젝트를 제거하는 코드를 넣는 것이 일반적입니다.
    Destroy(gameObject);
    }
}