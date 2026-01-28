using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 100f;
    public Transform cameraTransform;

    [Header("Look Settings")]
    public float minViewAngle = -90f; // 위를 보는 제한
    public float maxViewAngle = 70f;  // 아래를 보는 제한 (기존 90에서 70으로 수정)

    private float xRotation = 0f;
    private CharacterController controller;
    private Animator animator;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // 1. 시점 회전 로직
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;

        // [수정] 아래를 보는 각도를 70도로 제한하여 몸통 뚫림 방지
        xRotation = Mathf.Clamp(xRotation, minViewAngle, maxViewAngle);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);

        // 2. 이동 로직
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * moveSpeed * Time.deltaTime);

        bool isMoving = (x != 0 || z != 0);
        if (animator != null)
        {
            animator.SetBool("isMoving", isMoving);
        }
    }
}