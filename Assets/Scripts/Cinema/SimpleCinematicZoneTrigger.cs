using UnityEngine;
using UnityEngine.Playables;
using System.Collections.Generic;

public class SimpleCinematicZoneTrigger : MonoBehaviour
{
    [Header("Cameras")]
    public GameObject playerCamera;        // 플레이어 1인칭 카메라
    public GameObject cinematicRenderCam;  // 시네머신 브레인이 붙은 전용 카메라

    [Header("Characters")]
    public GameObject gameplayPlayer;      // 조작용 플레이어 (WalkingPlayer)
    public GameObject cinematicCharacter;  // 연출용 캐릭터 (Phone_Character)

    [Header("Timeline")]
    public PlayableDirector director;      // 타임라인 디렉터

    [Header("Objects to Toggle")]
    [Tooltip("시네마틱이 시작되는 순간 켤 오브젝트들. (원래 활성/비활성 상관없이 true로 만듭니다.)")]
    public List<GameObject> enableOnCinematicStart = new List<GameObject>();

    [Tooltip("시네마틱이 끝난 직후 켤 오브젝트들. Start()에서 자동으로 꺼둡니다.")]
    public List<GameObject> enableOnCinematicFinished = new List<GameObject>();

    private bool hasTriggered = false;

    void Start()
    {
        // 종료 이벤트 연결
        if (director != null) director.stopped += OnCinematicFinished;
        
        // 시네마틱용 오브젝트들은 처음에 꺼둡니다.
        if (cinematicRenderCam != null) cinematicRenderCam.SetActive(false);
        if (cinematicCharacter != null) cinematicCharacter.SetActive(false);

        SetActiveAll(enableOnCinematicFinished, false);
    }

    void OnDestroy()
    {
        if (director != null) director.stopped -= OnCinematicFinished;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어 태그 확인 및 중복 실행 방지
        if (!hasTriggered && other.CompareTag("Player"))
        {
            StartCinematic();
        }
    }

    void StartCinematic()
    {
        hasTriggered = true;

        // 1. 캐릭터 교체 및 위치 고정
        if (cinematicCharacter != null) cinematicCharacter.SetActive(true);
        if (gameplayPlayer != null) gameplayPlayer.SetActive(false);

        // 2. 카메라 교체
        if (playerCamera != null) playerCamera.SetActive(false);
        if (cinematicRenderCam != null) cinematicRenderCam.SetActive(true);

        SetActiveAll(enableOnCinematicStart, true);

        // 3. 타임라인 재생
        if (director != null) director.Play();
    }

    void OnCinematicFinished(PlayableDirector obj)
    {
        // 연출 종료 후 원래대로 복구
        if (cinematicCharacter != null) cinematicCharacter.SetActive(false);
        if (cinematicRenderCam != null) cinematicRenderCam.SetActive(false);
        
        if (playerCamera != null) playerCamera.SetActive(true);
        if (gameplayPlayer != null) gameplayPlayer.SetActive(true);

        SetActiveAll(enableOnCinematicFinished, true);
    }

    static void SetActiveAll(List<GameObject> list, bool active)
    {
        if (list == null) return;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null)
                list[i].SetActive(active);
        }
    }
}