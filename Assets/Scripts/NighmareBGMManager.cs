using UnityEngine;
using System.Collections;

public class NightmareBGMManager : MonoBehaviour
{
    public AudioSource bgmSource;
    public AudioClip nightmareBgmClip;
    
    [Header("Volume Settings")]
    public float maxVolume = 0.5f; // 목표 최대 볼륨
    public float fadeSpeed = 0.2f; // 볼륨이 커지는 속도
    
    private bool isBgmPlaying = false; // 현재 BGM이 재생 중인지 확인하는 플래그
    private Coroutine fadeCoroutine;

    void Start()
    {
        if (bgmSource == null) bgmSource = GetComponent<AudioSource>();
        if (bgmSource != null)
        {
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.volume = 0f; 
        }
    }

    void Update()
    {
        // 1. 가위눌림 플레이어의 활성화 상태 확인
        GameObject nightmarePlayer = GameObject.Find("NightMarePlayer");
        bool isNightmareActive = nightmarePlayer != null && nightmarePlayer.activeInHierarchy;

        // 2. GhostManager에 의해 귀신이 소환되었는지 확인
        bool isGhostActive = GhostManager.Instance != null && GhostManager.Instance.CurrentActiveGhost != null;
        
        // 3. 현재 날짜 확인
        int currentDay = SpawnManager.Instance != null ? SpawnManager.Instance.currentDay : 1;

        // 1일차라면 가위눌림이 켜지자마자 재생하고, 2일차부터는 귀신이 나타났을 때만 재생합니다.
        bool playCondition = false;
        if (isNightmareActive)
        {
            if (currentDay == 1) playCondition = true;
            else if (isGhostActive) playCondition = true;
        }

        if (playCondition && !isBgmPlaying)
        {
            isBgmPlaying = true;
            PlayBGM();
        }
        else if (!playCondition && isBgmPlaying)
        {
            isBgmPlaying = false;
            StopBGM();
        }
    }

    void PlayBGM()
    {
        if (bgmSource != null && nightmareBgmClip != null)
        {
            bgmSource.clip = nightmareBgmClip;
            bgmSource.volume = 0f; // 0부터 시작
            bgmSource.Play();
            
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeInRoutine()); 
        }
    }

    IEnumerator FadeInRoutine()
    {
        // 볼륨이 maxVolume(0.5)까지 서서히 커집니다.
        while (bgmSource.volume < maxVolume)
        {
            bgmSource.volume += fadeSpeed * Time.deltaTime; 
            yield return null;
        }
        bgmSource.volume = maxVolume; 
    }

    void StopBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            bgmSource.Stop();
            bgmSource.volume = 0f; 
        }
    }
}