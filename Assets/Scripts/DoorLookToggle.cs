using UnityEngine;

/// <summary>
/// 플레이어 시야(지정된 카메라의 화면 중앙 raycast)에서 바라보고 있는 동안
/// `E`를 누르면 문 상태를 열림/닫힘으로 토글합니다.
///
/// 적용 방법:
/// 1) 문(또는 문 오브젝트의 자식) Collider가 raycast에 맞도록 계층을 구성
/// 2) 이 스크립트를 문 오브젝트(애니 파라미터를 제어하는 위치)에 붙임
/// 3) Animator의 bool 파라미터 이름(openBoolName)을 맞춤 (기본: isOpen_Obj_1)
/// 4) walkingCamera에 WalkingPlayer의 카메라를 지정
/// </summary>
public class DoorLookToggle : MonoBehaviour
{
    [Header("Look & Interact")]
    [Tooltip("WalkingPlayer의 카메라를 지정하세요. (ItemInteraction과 같은 카메라 권장)")]
    public Camera walkingCamera;

    [Header("Player (for close distance)")]
    [Tooltip("닫기 거리 판정에 사용할 플레이어 Transform. 비워두면 tag=Player로 찾습니다.")]
    public Transform player;

    [Tooltip("화면 중앙에서 raycast를 쏩니다. 기본값은 (0.5, 0.5, 0).")]
    public Vector3 viewportRayOrigin = new Vector3(0.5f, 0.5f, 0f);

    [Tooltip("문을 바라볼 때 인식되는 최대 거리")]
    public float interactDistance = 3.0f;

    [Tooltip("true면 거리 제한 없이 시야(raycast)에 문이 잡히면 토글합니다.")]
    public bool ignoreDistanceLimit = false;

    [Tooltip("이 거리 안에 들어오면 문을 닫을 수 있습니다. (Day2DoorTrigger와 동일한 방식: 거리 + E)")]
    public float closeDistance = 3.0f;

    [Tooltip("raycast로 맞는 레이어만 상호작용합니다. 기본값은 Everything입니다.")]
    public LayerMask interactLayers = ~0;

    [Tooltip("문 콜라이더가 Trigger일 때도 raycast로 감지하려면 Collide로 설정하세요.")]
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    public KeyCode interactKey = KeyCode.E;

    [Header("Animator / Audio")]
    [Tooltip("Animator의 bool 파라미터 이름. 기존 프로젝트 문 제어가 기본적으로 'isOpen_Obj_1' 입니다.")]
    public string openBoolName = "Door_Ani";

    [Tooltip("시작 시 열림 상태로 만들지 여부")]
    public bool startOpen = false;

    [Tooltip("토글될 때 콘솔 로그를 출력할지 여부")]
    public bool logToggle = false;

    [Tooltip("바라보는 대상/거리/히트 정보를 콘솔에 출력할지 여부")]
    public bool logLookDebug = false;

    private Animator _anim;
    private AudioEmitter _audioEmitter;
    private bool _hasOpened = false;
    private bool _isClosed = false;

    private void Awake()
    {
        if (walkingCamera == null)
            walkingCamera = Camera.main;

        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }

        _anim = GetComponent<Animator>();
        _audioEmitter = GetComponent<AudioEmitter>();

        // "씬에서는 닫혀 있는데 플레이하면 열려 보임" 문제를 막기 위해,
        // 런타임 시작 시점에 startOpen 값을 Animator 파라미터에 강제로 반영합니다.
        _hasOpened = startOpen;
        _isClosed = !startOpen;
        if (_anim != null && !string.IsNullOrWhiteSpace(openBoolName))
        {
            _anim.enabled = true;
            _anim.SetBool(openBoolName, startOpen);
        }
    }

    private void Update()
    {
        if (walkingCamera == null)
            walkingCamera = Camera.main;

        if (walkingCamera == null || !walkingCamera.gameObject.activeInHierarchy)
            return;

        // 1) 열림: 문을 "바라보고" E를 누르면 한 번만 열림
        if (!_hasOpened)
        {
            Ray ray = walkingCamera.ViewportPointToRay(viewportRayOrigin);
            float maxDistance = ignoreDistanceLimit ? Mathf.Infinity : interactDistance;
            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactLayers, triggerInteraction))
            {
                if (logLookDebug)
                    Debug.Log($"[DoorLookToggle] No hit. cam={walkingCamera.name}, maxDist={maxDistance}");
                return;
            }

            if (!IsHitByThisDoor(hit.transform))
            {
                if (logLookDebug)
                    Debug.Log($"[DoorLookToggle] Hit other: {hit.transform.name} (root: {hit.transform.root.name}), dist={hit.distance:0.00}");
                return;
            }

            if (logLookDebug)
                Debug.Log($"[DoorLookToggle] Looking at door (to open): {name}, dist={hit.distance:0.00}, key={interactKey}");

            if (Input.GetKeyDown(interactKey))
            {
                OpenDoorOnce();
            }

            return;
        }

        // 2) 닫힘: 열렸고 아직 닫히지 않았으며, 플레이어가 closeDistance 안에 있을 때 E로 한 번만 닫힘
        if (_hasOpened && !_isClosed && player != null)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= closeDistance && Input.GetKeyDown(interactKey))
            {
                CloseDoorOnce();
            }
        }
    }

    private bool IsHitByThisDoor(Transform hitTransform)
    {
        if (hitTransform == null) return false;
        return hitTransform == transform || hitTransform.IsChildOf(transform);
    }

    private void OpenDoorOnce()
    {
        _hasOpened = true;
        _isClosed = false;

        if (_anim != null && !string.IsNullOrWhiteSpace(openBoolName))
        {
            _anim.enabled = true; // 일부 문 애니메이터는 비활성화 상태에서 bool만 갱신하면 안 될 수 있어 켬
            _anim.SetBool(openBoolName, true);
        }

        if (_audioEmitter != null)
            _audioEmitter.StartPlayback();

        if (logToggle)
            Debug.Log($"[DoorLookToggle] {name} -> OPEN");
    }

    private void CloseDoorOnce()
    {
        _isClosed = true;

        if (_anim != null && !string.IsNullOrWhiteSpace(openBoolName))
        {
            _anim.enabled = true;
            _anim.SetBool(openBoolName, false);
        }

        if (_audioEmitter != null)
            _audioEmitter.StartPlayback();

        if (logToggle)
            Debug.Log($"[DoorLookToggle] {name} -> CLOSE");
    }
}

