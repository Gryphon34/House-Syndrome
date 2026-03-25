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
    public GhostType myGhostType;

    [Header("Periodic Sound Settings")]
    public AudioSource ghostAudioSource;
    public AudioClip ghostSoundClip;
    public float minSoundInterval = 5f; // 최소 간격
    public float maxSoundInterval = 10f; // 최대 간격
    private float nextSoundTime;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    protected virtual void Start() 
    {
        if (playerTarget != null && agent != null)
        {
            agent.SetDestination(playerTarget.position);
        }
        // 첫 소리 재생 시간 설정
        nextSoundTime = Time.time + Random.Range(minSoundInterval, maxSoundInterval);
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

        // 주기적인 소리 재생 로직
        HandlePeriodicSound();
    }

    private void HandlePeriodicSound()
    {
        if (ghostAudioSource != null && ghostSoundClip != null && Time.time >= nextSoundTime)
        {
            ghostAudioSource.PlayOneShot(ghostSoundClip);
            nextSoundTime = Time.time + Random.Range(minSoundInterval, maxSoundInterval);
        }
    }

    protected void CatchPlayer()
    {
        if (isPlayerAwake) return;
        
        HandInputSystem[] allHands = FindObjectsByType<HandInputSystem>(FindObjectsSortMode.None);
        foreach (var hand in allHands) hand.StopAllCoroutines();

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
        Destroy(gameObject);
    }
}