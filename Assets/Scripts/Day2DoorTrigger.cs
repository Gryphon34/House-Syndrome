using UnityEngine;

public class Day2DoorTrigger : MonoBehaviour
{
    private Animator anim;
    private AudioEmitter audioEmitter;
    
    [Header("References")]
    public Transform player;          // [중요] 여기에 Player(WalkingPlayer)를 넣어야 합니다.
    
    [Header("Settings")]
    public float closeDistance = 3.0f; // 이 거리 안에 들어오면 문을 닫을 수 있음
    
    private bool hasOpened = false;   
    private bool isClosed = false;    

    void Start()
    {
        anim = GetComponent<Animator>();
        audioEmitter = GetComponent<AudioEmitter>();
        
        // 만약 수동으로 Player를 안 넣었다면, 태그로 찾음
        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }
    }

    void Update()
    {
        // 2일차 체크
        if (DifficultyManager.Instance != null && DifficultyManager.Instance.currentDay == 2)
        {
            // 1. 자동 열림 (씻기 완료)
            if (!hasOpened && SleepRuleManager.Instance != null && SleepRuleManager.Instance.CanSleep)
            {
                OpenDoorAutomatically();
            }

            // 2. 거리 기반 수동 닫기
            if (hasOpened && !isClosed && player != null)
            {
                float dist = Vector3.Distance(transform.position, player.position);
                
                // 지정된 거리 안에 있고 'E' 키를 누르면
                if (dist <= closeDistance && Input.GetKeyDown(KeyCode.E))
                {
                    CloseDoor();
                }
            }
        }
    }

    void OpenDoorAutomatically()
    {
        hasOpened = true;
        Debug.Log("<color=cyan>[Day2Door] 규칙 완료! 문이 열립니다.</color>");
        if (audioEmitter != null) audioEmitter.StartPlayback();
        if (anim != null) { anim.enabled = true; anim.SetBool("isOpen_Obj_1", true); }
    }

    void CloseDoor()
    {
        isClosed = true;
        Debug.Log("<color=green>[Day2Door] 거리 안에서 E 키 입력! 문을 닫습니다.</color>");
        if (anim != null) anim.SetBool("isOpen_Obj_1", false);
        if (audioEmitter != null) audioEmitter.StartPlayback();
    }
}