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
        // [추가] 각 귀신별 등장 사운드 (GDC 번들의 Jumpscare나 Sting 소스 추천)
        public AudioClip spawnSound; 
    }

    [Header("Day-by-Day Ghost Settings")]
    public DayGhostSettings[] daySettings = new DayGhostSettings[7];

    [Header("Audio")]
    public AudioSource audioSource; // 등장 소리를 재생할 오디오 소스

    private GameObject currentActiveGhost;
    public GameObject CurrentActiveGhost => currentActiveGhost;

    private bool isSpawning = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        GameObject nightmarePlayer = GameObject.Find("NightMarePlayer");
        bool isNightmareActive = nightmarePlayer != null && nightmarePlayer.activeInHierarchy;

        if (isNightmareActive && currentActiveGhost == null && !isSpawning)
        {
            StartCoroutine(SpawnAfterSevenSeconds(nightmarePlayer.transform));
        }

        if (!isNightmareActive) isSpawning = false;
    }

    IEnumerator SpawnAfterSevenSeconds(Transform target)
    {
        isSpawning = true;
        Debug.Log("<color=orange>[GhostManager] 7초 후 귀신 생성 예정...</color>");

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
            currentActiveGhost = Instantiate(settings.ghostPrefab, settings.spawnPoint.position, settings.spawnPoint.rotation);

            // [핵심] 귀신 등장 사운드 재생
            if (audioSource != null && settings.spawnSound != null)
            {
                audioSource.PlayOneShot(settings.spawnSound);
            }

            var ghostLogic = currentActiveGhost.GetComponent<NavMeshGhostBase>();
            if (ghostLogic != null) ghostLogic.SetTarget(target);
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