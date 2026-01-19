using UnityEngine;

public class HeadLookController : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public Transform playerHead; // mixamorig:Head 할당

    float xRotation = 0f;
    float yRotation = 0f;

    void Start()
    {
        // 마우스 커서를 화면 중앙에 고정
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yRotation += mouseX;
        xRotation -= mouseY;

        // 고개가 돌아가는 각도 제한 (누워있는 시점 최적화)
        xRotation = Mathf.Clamp(xRotation, -30f, 30f); // 위아래
        yRotation = Mathf.Clamp(yRotation, -60f, 60f); // 좌우 (손을 보기 위해)

        playerHead.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }
}