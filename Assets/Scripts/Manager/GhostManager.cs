using UnityEngine;
using System.Collections;

public class GhostManager : MonoBehaviour
{
    public static GhostManager Instance;

    [System.Serializable]
    public class DayGhostSettings
    {
        public string dayName;           // 요일 이름 (확인용)
        public GameObject ghostPrefab;   // 소환할 귀신 프리팹
        public Transform spawnPoint;     // 소환될 위치
    }

    [Header("Day-by-Day Ghost Settings")]
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
        // 밤몽 플레이어가 활성화되어 있을 때만 소환 체크
        GameObject nightmarePlayer = GameObject.Find("NightMarePlayer");
        bool isNightmareActive = nightmarePlayer != null && nightmarePlayer.activeInHierarchy;

        if (isNightmareActive && currentActiveGhost == null && !isSpawning)
        {
            StartCoroutine(SpawnAfterSevenSeconds(nightmarePlayer.transform));
        }

        if (!isNightmareActive)
        {
            isSpawning = false;
        }
    }

    IEnumerator SpawnAfterSevenSeconds(Transform target)
    {
        isSpawning = true;
        Debug.Log("<color=orange>[GhostManager] 가위눌림 시작: 7초 후 귀신이 생성됩니다.</color>");

        yield return new WaitForSeconds(7f);

        if (target != null && target.gameObject.activeInHierarchy)
        {
            SpawnGhostForCurrentDay(target);
        }
    }

    void SpawnGhostForCurrentDay(Transform target)
    {
        // SpawnManager의 현재 날짜를 기준으로 설정 가져오기
        int dayIndex = Mathf.Clamp(SpawnManager.Instance.currentDay - 1, 0, daySettings.Length - 1);
        DayGhostSettings settings = daySettings[dayIndex];

        if (settings.ghostPrefab != null && settings.spawnPoint != null)
        {
            // 귀신 생성
            currentActiveGhost = Instantiate(settings.ghostPrefab, settings.spawnPoint.position, settings.spawnPoint.rotation);

            // [NavMesh 방식] 타겟만 설정해주면 귀신이 스스로 길을 찾아갑니다.
            var ghostLogic = currentActiveGhost.GetComponent<NavMeshGhostBase>();
            if (ghostLogic != null)
            {
                ghostLogic.SetTarget(target);
            }
            else
            {
                Debug.LogWarning($"{settings.ghostPrefab.name}에 NavMeshGhostBase 스크립트가 없습니다!");
            }
        }
    }

    public void ClearGhost()
    {
        if (currentActiveGhost != null) 
        { 
            Destroy(currentActiveGhost); 
            currentActiveGhost = null; 
        }
        isSpawning = false;
    }
}