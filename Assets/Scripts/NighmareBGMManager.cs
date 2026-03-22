using UnityEngine;
using System.Collections;

public class NightmareBGMManager : MonoBehaviour
{
    public AudioSource bgmSource;
    public AudioClip nightmareBgmClip;
    
    [Header("Volume Settings")]
    public float maxVolume = 0.5f; // 목표 최대 볼륨 (0.5로 제한)
    public float fadeSpeed = 0.2f; // 볼륨이 커지는 속도 (낮을수록 천천히 커짐)
    
    private bool wasNightmareActive = false;
    private Coroutine fadeCoroutine;

    void Start()
    {
        if (bgmSource == null) bgmSource = GetComponent<AudioSource>();
        if (bgmSource != null)
        {
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.volume = 0f; // 시작 시 볼륨을 0으로 설정
        }
    }

    void Update()
    {
        // NightMarePlayer 오브젝트의 활성화 상태를 감시합니다.
        GameObject nightmarePlayer = GameObject.Find("NightMarePlayer");
        bool isNightmareActive = nightmarePlayer != null && nightmarePlayer.activeInHierarchy;

        // 가위눌림이 시작되는 순간 (잠에 들 때)
        if (isNightmareActive && !wasNightmareActive)
        {
            PlayBGM();
        }
        // 가위눌림이 끝나는 순간 (잠에서 깰 때)
        else if (!isNightmareActive && wasNightmareActive)
        {
            StopBGM();
        }

        wasNightmareActive = isNightmareActive;
    }

    void PlayBGM()
    {
        if (bgmSource != null && nightmareBgmClip != null)
        {
            bgmSource.clip = nightmareBgmClip;
            bgmSource.volume = 0f; 
            bgmSource.Play();
            
            // 이미 실행 중인 페이드 코루틴이 있다면 중지하고 새로 시작합니다.
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeInRoutine()); 
        }
    }

    IEnumerator FadeInRoutine()
    {
        // 현재 볼륨이 maxVolume(0.5)에 도달할 때까지 서서히 증가시킵니다.
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
            bgmSource.volume = 0f; // 볼륨 초기화
        }
    }
}