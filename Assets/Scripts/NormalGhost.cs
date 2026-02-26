using UnityEngine;

public class NormalGhost : MonoBehaviour
{
    [Header("Movement Settings")]
    public Transform playerTarget;
    public float timeToReach = 30f;
    public float catchDistance = 1f; // 플레이어와 0.5m 이내면 잡힌 것으로 간주

    // [에러 해결] GhostManager에서 참조할 수 있도록 변수를 다시 추가합니다.
    [HideInInspector] public float startDelay = 0f;

    private Transform[] waypoints;
    private float timer = 0f;
    private Vector3 lastPosition;
    private bool isPlayerAwake = false;

    void Start()
    {
        lastPosition = transform.position;
        // 생성 즉시 보이게 설정 (매니저가 이미 7초를 기다렸으므로)
        SetGhostVisibility(true);

        if (playerTarget == null)
        {
            GameObject playerObj = GameObject.Find("NightMarePlayer");
            if (playerObj != null) playerTarget = playerObj.transform;
        }
    }

    public void SetTarget(Transform target) { playerTarget = target; }
    public void SetPath(Transform[] path) { waypoints = path; }

    void Update()
    {
        if (isPlayerAwake || playerTarget == null) return;

        timer += Time.deltaTime;
        float segmentTime = timeToReach / (waypoints != null && waypoints.Length > 0 ? waypoints.Length : 1);

        // 1. 이동 로직
        if (waypoints == null || waypoints.Length == 0)
        {
            transform.position = Vector3.Lerp(lastPosition, playerTarget.position, timer / timeToReach);
        }
        else
        {
            MoveAlongPath(segmentTime);
        }

        // 2. 거리 기반 실패 체크
        float currentDistance = Vector3.Distance(transform.position, playerTarget.position);
        if (currentDistance <= catchDistance)
        {
            CatchPlayer();
        }
    }

    void MoveAlongPath(float segmentTime)
    {
        float progress = (timer % segmentTime) / segmentTime;
        int index = Mathf.FloorToInt(timer / segmentTime);

        if (index < waypoints.Length)
        {
            Vector3 start = (index == 0) ? lastPosition : waypoints[index - 1].position;
            Vector3 end = waypoints[index].position;
            transform.position = Vector3.Lerp(start, end, progress);
        }
        else
        {
            transform.position = Vector3.Lerp(waypoints[waypoints.Length - 1].position, playerTarget.position, (timer - timeToReach) / 2f);
        }
    }

    void CatchPlayer()
    {
        if (isPlayerAwake) return;
        Debug.Log("<color=red>귀신에게 잡혔습니다! 전날로 돌아갑니다.</color>");

        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.ReturnToPreviousDay(); // [실패] 전날로 이동
        }
    }

    void SetGhostVisibility(bool visible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = visible;
    }

    public void OnPlayerWakeUp() { isPlayerAwake = true; Destroy(gameObject, 0.5f); }
}