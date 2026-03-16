using UnityEngine;

public class HeadLookController : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public Transform playerHead; // mixamorig:Head �Ҵ�
    public bool isLocked = false; // [추가] 시선 잠금 플래그

    float xRotation = 0f;
    float yRotation = 0f;

    void Start()
    {
        // ���콺 Ŀ���� ȭ�� �߾ӿ� ����
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // 시선이 잠겨있으면 마우스 입력을 받지 않습니다.
        if (isLocked) return; 

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yRotation += mouseX;
        xRotation -= mouseY;

        // ������ ���ư��� ���� ���� (�����ִ� ���� ����ȭ)
        xRotation = Mathf.Clamp(xRotation, -30f, 30f); // ���Ʒ�
        yRotation = Mathf.Clamp(yRotation, -60f, 60f); // �¿� (���� ���� ����)

        playerHead.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }
}