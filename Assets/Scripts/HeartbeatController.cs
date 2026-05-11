using UnityEngine;

public class HeartbeatController : MonoBehaviour
{
    public AudioSource heartbeatSource; // 심장 소리가 재생될 오디오 소스
    public float maxDistance = 15f;     // 소리가 들리기 시작하는 최대 거리
    public float minPitch = 1.0f;       // 가장 멀 때의 속도
    public float maxPitch = 2.5f;       // 가장 가까울 때의 속도 (빠름)

    private Transform playerTransform;

    void Start()
    {
        // 씬에서 플레이어를 찾습니다.
        GameObject playerObj = GameObject.Find("NightMarePlayer");
        if (playerObj != null) playerTransform = playerObj.transform;
    }

    void Update()
    {
        if (playerTransform == null || GhostManager.Instance == null) return;

        // GhostManager를 통해 현재 활성화된 귀신을 가져옵니다
        GameObject activeGhost = GhostManager.Instance.CurrentActiveGhost;

        if (activeGhost != null && heartbeatSource != null)
        {
            // 귀신과 플레이어 사이의 거리 계산
            float distance = Vector3.Distance(activeGhost.transform.position, playerTransform.position);

            // 거리를 0~1 사이의 가중치로 변환 (가까울수록 1에 가까워짐)
            float t = Mathf.Clamp01(1f - (distance / maxDistance));

            // 가중치에 따라 피치와 볼륨 조절
            heartbeatSource.pitch = Mathf.Lerp(minPitch, maxPitch, t);
            heartbeatSource.volume = Mathf.Lerp(0f, 1f, t);

            // 소리가 꺼져 있다면 재생
            if (!heartbeatSource.isPlaying) heartbeatSource.Play();
        }
        else if (heartbeatSource != null && heartbeatSource.isPlaying)
        {
            // 귀신이 없으면 서서히 소리를 줄이며 정지
            heartbeatSource.volume = Mathf.Lerp(heartbeatSource.volume, 0f, Time.deltaTime * 2f);
            if (heartbeatSource.volume < 0.05f) heartbeatSource.Stop();
        }
    }
}