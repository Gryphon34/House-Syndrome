using UnityEngine;
using UnityEngine.Serialization;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private string horizontalInputName = "Horizontal";
    [SerializeField] private string verticalInputName = "Vertical";
    [FormerlySerializedAs("moveSpeed")]
    public float movementSpeed = 5f;
    [Tooltip("If true uses CharacterController.SimpleMove (auto deltaTime + gravity). Otherwise uses Move.")]
    public bool useSimpleMove = true;

    [Header("Look")]
    [SerializeField] private string mouseXInputName = "Mouse X";
    [SerializeField] private string mouseYInputName = "Mouse Y";
    public float mouseSensitivity = 100f;
    public Transform cameraTransform;

    [Header("Look Settings")]
    public float minViewAngle = -90f; // 占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙占쏙옙
    public float maxViewAngle = 70f;

    private float xRotation = 0f;
    private CharacterController controller;

    /// <summary> 카메라 현재 피치를 xRotation에 반영. (CameraDownLook 애니 종료 후 동기화용) </summary>
    public void SyncPitchFromCamera()
    {
        if (cameraTransform == null) return;
        float x = cameraTransform.localEulerAngles.x;
        if (x > 180f) x -= 360f;
        xRotation = Mathf.Clamp(x, minViewAngle, maxViewAngle);
    }
    private Animator animator;
    private bool cursorLocked = true;
    [Header("Animation")]
    [SerializeField] private string isMovingBoolName = "isMoving";

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        if (cameraTransform == null)
        {
            Camera childCam = GetComponentInChildren<Camera>();
            if (childCam != null) cameraTransform = childCam.transform;
        }

        // Initialize pitch from current camera local rotation (prevents snapping).
        if (cameraTransform != null)
        {
            float initialX = cameraTransform.localEulerAngles.x;
            if (initialX > 180f) initialX -= 360f;
            xRotation = initialX;
        }

        ApplyCursorLockState(true);
    }

    void Update()
    {
        HandleCursorLockToggle();
        HandleLook();
        HandleMovementAndAnimation();
    }

    private void HandleCursorLockToggle()
    {
        // Escape unlocks cursor, left click re-locks (same behavior as PlayerLook.cs).
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            ApplyCursorLockState(false);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            ApplyCursorLockState(true);
        }
    }

    private void ApplyCursorLockState(bool locked)
    {
        cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void HandleLook()
    {
        if (cameraTransform == null) return;

        float mouseX = Input.GetAxis(mouseXInputName) * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis(mouseYInputName) * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minViewAngle, maxViewAngle);

        // 아래 보기 애니메이션 중에는 카메라 피치를 건드리지 않음 (애니 종료 후 다시 적용됨)
        if (!CameraDownLook.IsAnimating)
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleMovementAndAnimation()
    {
        if (controller == null) return;

        float x = Input.GetAxis(horizontalInputName);
        float z = Input.GetAxis(verticalInputName);

        Vector3 move = (transform.right * x) + (transform.forward * z);

        if (useSimpleMove)
        {
            // SimpleMove applies gravity and deltaTime internally.
            controller.SimpleMove(move * movementSpeed);
        }
        else
        {
            controller.Move(move * movementSpeed * Time.deltaTime);
        }

        if (animator != null && !string.IsNullOrWhiteSpace(isMovingBoolName))
        {
            bool isMoving = !Mathf.Approximately(x, 0f) || !Mathf.Approximately(z, 0f);
            animator.SetBool(isMovingBoolName, isMoving);
        }
    }
}
