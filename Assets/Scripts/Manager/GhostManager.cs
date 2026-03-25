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
        public AudioClip spawnSound; 
    }

    [Header("Day-by-Day Ghost Settings")]
    public DayGhostSettings[] daySettings = new DayGhostSettings[7];

    [Header("Audio")]
    public AudioSource audioSource; 
    public AudioClip preSpawnBGM; // 귀신이 나타나기 전 7초 동안 흐를 긴장감 사운드

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

        // SpawnManager에서 현재 날짜를 확인하여 1일차보다 클 때만 스폰 로직이 작동하게 합니다.
        if (isNightmareActive && currentActiveGhost == null && !isSpawning && SpawnManager.Instance.currentDay > 1)
        {
            StartCoroutine(SpawnAfterSevenSeconds(nightmarePlayer.transform));
        }

        if (!isNightmareActive)
        {
            isSpawning = false;
            if (audioSource != null && audioSource.isPlaying && currentActiveGhost == null)
            {
                audioSource.Stop();
            }
        }
    }

    IEnumerator SpawnAfterSevenSeconds(Transform target)
    {
        isSpawning = true;
        Debug.Log("<color=orange>[GhostManager] 7초 후 귀신 생성 예정...</color>");

        // [추가] 귀신이 나타나기 전 긴장감 조성 배경음 재생
        if (audioSource != null && preSpawnBGM != null)
        {
            audioSource.clip = preSpawnBGM;
            audioSource.loop = true; // 7초 동안 반복 재생
            audioSource.Play();
        }

        yield return new WaitForSeconds(7f);

        if (target != null && target.gameObject.activeInHierarchy)
        {
            SpawnGhostForCurrentDay(target);
        }
    }

    void SpawnGhostForCurrentDay(Transform target)
    {
        // [추가] 귀신이 나타나는 순간 대기음 중지
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false; // 효과음을 위해 루프 해제
        }

        int dayIndex = Mathf.Clamp(SpawnManager.Instance.currentDay - 1, 0, daySettings.Length - 1);
        DayGhostSettings settings = daySettings[dayIndex];

        if (settings.ghostPrefab != null && settings.spawnPoint != null)
        {
            currentActiveGhost = Instantiate(settings.ghostPrefab, settings.spawnPoint.position, settings.spawnPoint.rotation);

            // [핵심] 귀신 등장 효과음(Sting) 재생
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
        
        // 귀신 제거 시 오디오 정리
        if (audioSource != null) audioSource.Stop();
    }
}