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
        if (SpawnManager.Instance != null) SpawnManager.Instance.ReturnToPreviousDay();
    }

    public virtual void OnPlayerWakeUp()
    {
        isPlayerAwake = true;
        if (agent != null) agent.isStopped = true;
        Destroy(gameObject, 0.5f);
    }
}