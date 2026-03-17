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
        public Transform spawnPoint;
    }

    [Header("Day-by-Day Ghost Settings")]
    public DayGhostSettings[] daySettings = new DayGhostSettings[7];

    // 수정된 부분: 실제 값을 담는 변수는 private으로, 외부에 보여주는 통로는 public으로 만듭니다.
    private GameObject currentActiveGhost;
    public GameObject CurrentActiveGhost => currentActiveGhost;

    private bool isSpawning = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
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
        int dayIndex = Mathf.Clamp(SpawnManager.Instance.currentDay - 1, 0, daySettings.Length - 1);
        DayGhostSettings settings = daySettings[dayIndex];

        if (settings.ghostPrefab != null && settings.spawnPoint != null)
        {
            // 이제 currentActiveGhost 변수에 정상적으로 값을 할당할 수 있습니다.
            currentActiveGhost = Instantiate(settings.ghostPrefab, settings.spawnPoint.position, settings.spawnPoint.rotation);

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