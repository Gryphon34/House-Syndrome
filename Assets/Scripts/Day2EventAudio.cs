using UnityEngine;
using System.Collections;

public class Day2EventAudio : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource eventAudioSource; // 배경음을 재생할 오디오 소스
    public AudioClip ghostAtmosphereClip; // 재생할 으스스한 배경음
    public float delaySeconds =1.5f;

    private bool isTimerRunning =false;
    private bool hasPlayedThisNight = false;
    void OnEnable()
    {
        // 화장실 핸들 사용 이벤트 구독
        SleepRuleManager.OnBathroomHandleUsed += CheckAndStartTimer;
        SpawnManager.OnDayChangedEvent+=ResetStatus;
    }

    void OnDisable()
    {
        // 이벤트 구독 해제
        SleepRuleManager.OnBathroomHandleUsed -= CheckAndStartTimer;
        SpawnManager.OnDayChangedEvent -= ResetStatus;
    }

    void ResetStatus()
    {
        hasPlayedThisNight = false;
        isTimerRunning = false;
    }

    void CheckAndStartTimer()
    {
        // 2일차이고, 핸들을 껐으며(IsHandleTurnedOff), 아직 소리가 재생 예약되지 않았을 때만 실행
        if (SpawnManager.Instance != null && SpawnManager.Instance.currentDay == 2)
        {
            if (SleepRuleManager.Instance.IsHandleTurnedOff && !isTimerRunning && !hasPlayedThisNight)
            {
            StartCoroutine(DelayedPlayRoutine());
            }
        }
    }

    IEnumerator DelayedPlayRoutine()
    {
        isTimerRunning = true;
        Debug.Log("[Day2Event] 핸들이 꺼짐. 2초 후 사운드 재생 예정");

        yield return new WaitForSeconds(delaySeconds);

        if (eventAudioSource != null && ghostAtmosphereClip != null)
        {
            eventAudioSource.clip = ghostAtmosphereClip;
            eventAudioSource.loop = true;
            eventAudioSource.Play();
            hasPlayedThisNight = true;
            Debug.Log("[Day2Event] 2초 지연 후 배경음 재생 시작");
        }
    
        isTimerRunning = false;
    }
        void Update()
    {
        GameObject nightmarePlayer = GameObject.Find("NightMarePlayer");
        bool isNightmareActive = nightmarePlayer != null && nightmarePlayer.activeInHierarchy;

        // 잠에 들어 NightmarePlayer가 켜지면 소리를 즉시 정지
        if (isNightmareActive && eventAudioSource.isPlaying)
        {
            eventAudioSource.Stop();
            Debug.Log("[Day2Event] 잠에 들어 배경음 종료");
        }
    }
}