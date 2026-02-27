using UnityEngine;
using System.Collections;

public class GhostManager : MonoBehaviour
{
    public static GhostManager Instance;

    [System.Serializable]
    public class DayGhostSettings
    {
        public string dayName;
        public GameObject ghostPrefab;
        public Transform[] movePath;
        public Transform spawnPoint;
    }

    public DayGhostSettings[] daySettings = new DayGhostSettings[7];
    private GameObject currentActiveGhost;
    private bool isSpawning = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        // [핵심 수정] NightMarePlayer 오브젝트가 활성화되었는지 직접 확인합니다.
        GameObject nightmarePlayer = GameObject.Find("NightMarePlayer");
        bool isNightmareActive = nightmarePlayer != null && nightmarePlayer.activeInHierarchy;

        // 밤이고, NightMarePlayer가 켜졌으며, 아직 귀신이 없을 때 7초 대기 시작
        if (isNightmareActive && currentActiveGhost == null && !isSpawning)
        {
            StartCoroutine(SpawnAfterSevenSeconds(nightmarePlayer.transform));
        }

        // 가위에서 깨어나면(NightMarePlayer가 꺼지면) 상태 리셋
        if (!isNightmareActive)
        {
            isSpawning = false;
        }
    }

    IEnumerator SpawnAfterSevenSeconds(Transform target)
    {
        isSpawning = true;
        Debug.Log("<color=orange>NightMarePlayer 활성화 감지! 7초 뒤 귀신이 나타납니다.</color>");

        yield return new WaitForSeconds(7f); // [핵심] 7초 대기

        // 7초 후에도 여전히 플레이어가 가위눌림 상태인지 재확인
        if (target != null && target.gameObject.activeInHierarchy)
        {
            SpawnGhostForCurrentDay(target);
        }
    }

    void SpawnGhostForCurrentDay(Transform target)
    {
        int dayIndex = Mathf.Clamp(SpawnManager.Instance.currentDay - 1, 0, daySettings.Length - 1);
        DayGhostSettings settings = daySettings[dayIndex];

        if (settings.ghostPrefab != null && settings.spawnPoint != null)
        {
            currentActiveGhost = Instantiate(settings.ghostPrefab, settings.spawnPoint.position, settings.spawnPoint.rotation);

            // 1. 일반 귀신인 경우 처리
            var normalGhost = currentActiveGhost.GetComponent<NormalGhost>();
            if (normalGhost != null)
            {
                normalGhost.SetPath(settings.movePath);
                normalGhost.SetTarget(target);
                normalGhost.startDelay = 0f;
            }

            // 2. [추가] 정지 귀신(StalkerGhost)인 경우 처리
            var stalkerGhost = currentActiveGhost.GetComponent<StalkerGhost>();
            if (stalkerGhost != null)
            {
                stalkerGhost.SetPath(settings.movePath);
                stalkerGhost.SetTarget(target);
                // StalkerGhost에는 startDelay가 없으므로 이 줄은 생략합니다.
            }
        }
    }

    public void ClearGhost()
    {
        if (currentActiveGhost != null) { Destroy(currentActiveGhost); currentActiveGhost = null; }
        isSpawning = false;
    }
}