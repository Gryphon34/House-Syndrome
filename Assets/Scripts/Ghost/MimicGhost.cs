using UnityEngine;
using System.Collections;

/// <summary>
/// 모방 귀신: NavMesh를 통해 접근하며, 특정 타이밍에 지인의 목소리를 흉내 내어 플레이어의 실수를 유도합니다.
/// </summary>
public class MimicGhost : NavMeshGhostBase
{
    [Header("Audio Sources")]
    public AudioSource voiceSource;
    public AudioClip sighClip;
    public AudioClip[] mimicClips;

    [Header("Mimic Settings")]
    public float mimicDuration = 5f;
    public float warningTime = 2f;
    
    private EyeBlinkController eyeBlink;
    private HandInputSystem handInput;
    private HeadLookController headLook;
    
    private bool isMimicking = false;
    private bool isPlayerCaught = false;

    protected override void Start()
    {
        base.Start(); // 부모 클래스의 타겟 설정 및 기본 초기화 수행

        eyeBlink = FindFirstObjectByType<EyeBlinkController>();
        handInput = FindFirstObjectByType<HandInputSystem>();
        headLook = FindFirstObjectByType<HeadLookController>();
        
        // 목소리 루틴 시작
        StartCoroutine(GhostRoutine());

        Debug.Log("<color=orange>[MimicGhost] 모방 귀신 스폰: 목소리로 플레이어를 유혹합니다.</color>");
    }

    protected override void Update()
    {
        if (isPlayerAwake || playerTarget == null) return;

        // 1. 모방 중일 때 플레이어 상태 감시 (눈을 떴는지 체크) 
        if (isMimicking) CheckPlayerStatus();

        // 2. 페널티 수행 중일 때 강제 회전 실행 (귀신을 강제로 보게 함) 
        if (isPlayerCaught && headLook != null)
        {
            LookAtGhost();
        }

        // 3. 거리 기반 잡기 판정 및 기본 속도 업데이트 (NavMeshGhostBase 기능 호출)
        base.Update();
    }

    IEnumerator GhostRoutine()
    {
        while (!isPlayerAwake)
        {
            yield return new WaitForSeconds(Random.Range(5f, 15f));
            
            // 전조 증상: 탄식 소리를 들려줌 [cite: 33]
            if (voiceSource != null) voiceSource.PlayOneShot(sighClip);
            yield return new WaitForSeconds(warningTime);

            // 모방 시작: 지인의 목소리 출력 [cite: 31]
            isMimicking = true;
            if (voiceSource != null && mimicClips.Length > 0)
            {
                voiceSource.clip = mimicClips[Random.Range(0, mimicClips.Length)];
                voiceSource.Play();
            }

            yield return new WaitForSeconds(mimicDuration);

            // 모방 종료 및 상태 복구
            isMimicking = false;
            isPlayerCaught = false;
        
            if (headLook != null) headLook.isLocked = false; 
            if (handInput != null) handInput.enabled = true; 
        }
    }

    void CheckPlayerStatus()
    {
        // 눈을 감으면 소리가 작아지고, 눈을 뜨면 처벌 [cite: 33]
        if (eyeBlink != null) voiceSource.volume = eyeBlink.eyeOpenAmount;

        if (eyeBlink != null && eyeBlink.eyeOpenAmount > 0.7f) ApplyPenalty();
    }

    void ApplyPenalty()
    {
        if (isPlayerCaught) return;
        isPlayerCaught = true;

        Debug.Log("<color=red>[Mimic] 처벌! 시선 고정 및 가위풀기 봉쇄!</color>");

        // 가위풀기 기능 및 UI 비활성화 
        if (handInput != null) 
        {
            handInput.enabled = false;
            handInput.uiParentGroup.SetActive(false); 
        }

        // 마우스 회전 잠금
        if (headLook != null) headLook.isLocked = true;
    }

    void LookAtGhost()
    {
        // 귀신의 가슴/얼굴 높이를 강제로 바라보게 설정
        Vector3 targetPos = transform.position + Vector3.up * 1.5f;
        Vector3 direction = (targetPos - headLook.playerHead.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
    
        headLook.playerHead.rotation = Quaternion.Slerp(headLook.playerHead.rotation, targetRotation, Time.deltaTime * 5f);
    }

    public override void OnPlayerWakeUp() 
    { 
        // 깨어날 때 마우스 잠금 해제 등 상태 초기화
        if (headLook != null) headLook.isLocked = false;
        base.OnPlayerWakeUp(); 
    }
}