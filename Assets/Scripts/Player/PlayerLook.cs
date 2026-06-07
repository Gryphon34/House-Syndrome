using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [SerializeField] private string mouseXInputName = "Mouse X";
    [SerializeField] private string mouseYInputName = "Mouse Y";
    [SerializeField] private float mouseSensitivity = 150f;

    [SerializeField] private Transform playerBody;
    private float xAxisClamp;
    private bool m_cursorIsLocked = true;

    // 감도 PlayerPrefs 키 (MainMenuManager와 동일하게 유지)
    public const string KeySensitivity = "MouseSensitivity";
    // 슬라이더 100% 일 때의 실제 감도 최댓값
    public const float MaxSensitivity = 300f;

    private void Awake()
    {
        // 저장된 감도 퍼센트(1~100)를 실제 감도값으로 변환해 적용
        float savedPercent = PlayerPrefs.GetFloat(KeySensitivity, 50f);
        mouseSensitivity = PercentToSensitivity(savedPercent);

        LockCursor();
        xAxisClamp = 0.0f;
    }

    /// <summary>퍼센트(1~100) → 실제 감도값 변환</summary>
    public static float PercentToSensitivity(float percent)
    {
        return (Mathf.Clamp(percent, 1f, 100f) / 100f) * MaxSensitivity;
    }

    /// <summary>런타임에 감도를 퍼센트(1~100)로 즉시 변경합니다.</summary>
    public void SetSensitivityPercent(float percent)
    {
        mouseSensitivity = PercentToSensitivity(percent);
    }

    private void LockCursor()
    {
       
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            m_cursorIsLocked = false;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            m_cursorIsLocked = true;
        }

        if (m_cursorIsLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else if (!m_cursorIsLocked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
    }

    private void Update()
    {
        CameraRotation();
    }

    private void CameraRotation()
    {
        float mouseX = Input.GetAxis(mouseXInputName) * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis(mouseYInputName) * mouseSensitivity * Time.deltaTime;

        xAxisClamp += mouseY;

        if (xAxisClamp > 90.0f)
        {
            xAxisClamp = 90.0f;
            mouseY = 0.0f;
            ClampXAxisRotationToValue(270.0f);
        }
        else if (xAxisClamp < -90.0f)
        {
            xAxisClamp = -90.0f;
            mouseY = 0.0f;
            ClampXAxisRotationToValue(90.0f);
        }

        transform.Rotate(Vector3.left * mouseY);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    private void ClampXAxisRotationToValue(float value)
    {
        Vector3 eulerRotation = transform.eulerAngles;
        eulerRotation.x = value;
        transform.eulerAngles = eulerRotation;
    }
}
