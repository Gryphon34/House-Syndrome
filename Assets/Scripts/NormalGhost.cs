using UnityEngine;
using UnityEngine.SceneManagement;

public class NormalGhost : MonoBehaviour
{
    [Header("Movement Settings")]
    public Transform playerTarget;      // NightmarePlayer 위치
    public float timeToReach = 30f;    // 플레이어에게 도달하는 데 걸리는 시간 (초)
    public float startDelay = 7f;      // 잠든 후 귀신이 나타나기까지의 대기 시간

    private Vector3 spawnPosition;
    private float timer = 0f;
    private float delayTimer = 0f;      // 대기용 타이머
    private bool isPlayerAwake = false;
    private bool isNightmareStarted = false; // 가위눌림 시작 여부
    private bool canStartMoving = false;    // 실제 이동 시작 여부

    void Start()
    {
        spawnPosition = transform.position;

        // 난이도에 따른 시간 조절
        if (DifficultyManager.Instance != null)
        {
            timeToReach -= (DifficultyManager.Instance.currentDay * 2f);
            timeToReach = Mathf.Max(10f, timeToReach);
        }

        // 시작 시에는 귀신을 숨겨둡니다. (MeshRenderer가 있다면)
        SetGhostVisibility(false);
    }

    void Update()
    {
        if (isPlayerAwake) return;

        // 1. 플레이어가 잠들었는지 확인 (NightmarePlayer가 활성화 되었을 때)
        if (!isNightmareStarted)
        {
            if (playerTarget != null && playerTarget.gameObject.activeInHierarchy)
            {
                isNightmareStarted = true;
                Debug.Log("플레이어가 잠들었습니다. 7초 뒤 귀신이 나타납니다.");
            }
            return; // 잠들기 전에는 아무것도 하지 않음
        }

        // 2. 잠든 후 7초 대기
        if (!canStartMoving)
        {
            delayTimer += Time.deltaTime;
            if (delayTimer >= startDelay)
            {
                canStartMoving = true;
                SetGhostVisibility(true); // 7초 뒤 귀신을 나타나게 함
                Debug.Log("귀신 이동 시작!");
            }
            return; // 7초가 지나기 전에는 이동하지 않음
        }

        // 3. 7초 대기 후 플레이어에게 서서히 다가감
        timer += Time.deltaTime;
        float progress = timer / timeToReach;

        transform.position = Vector3.Lerp(spawnPosition, playerTarget.position, progress);

        if (progress >= 1.0f)
        {
            CatchPlayer();
        }
    }

    // 귀신을 껐다 켰다 하는 유틸리티 함수
    void SetGhostVisibility(bool visible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = visible;
    }

    public void OnPlayerWakeUp()
    {
        isPlayerAwake = true;
        Destroy(gameObject, 1f);
    }

    void CatchPlayer()
    {
        // 실패 시 SpawnManager를 통해 전날로 돌아감
        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.ReturnToPreviousDay();
        }
    }
}