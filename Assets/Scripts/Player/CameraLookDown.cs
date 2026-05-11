using UnityEngine;
using System.Collections;

/// <summary>
/// bathroom_handle을 E키로 상호작용했을 때만 자동으로 카메라가 아래를 보았다가 원래대로 돌아오는 동작을 수행.
/// 카메라 localRotation(피치)만 사용해 PlayerController의 상하/좌우 룩과 충돌하지 않음.
/// </summary>
public class CameraDownLook : MonoBehaviour
{
    public float downAngle = 60f; // 아래로 볼 각도
    public float speed = 2f;      // 움직임 속도

    /// <summary> 아래 보기 애니메이션 중이면 true. PlayerController가 이 동안 카메라 피치를 건드리지 않도록 사용. </summary>
    public static bool IsAnimating { get; private set; }

    private bool isLookingDown = false;

    void OnEnable()
    {
        SleepRuleManager.OnBathroomHandleUsed += TriggerLookDown;
    }

    void OnDisable()
    {
        SleepRuleManager.OnBathroomHandleUsed -= TriggerLookDown;
    }

    void TriggerLookDown()
    {
        if (!isLookingDown)
            StartCoroutine(LookDownAndBack());
    }

    IEnumerator LookDownAndBack()
    {
        isLookingDown = true;
        IsAnimating = true;

        // 카메라 local X(피치)만 사용. 좌우 회전은 부모(PlayerController)가 담당하므로 건드리지 않음
        float originalPitch = transform.localEulerAngles.x;
        if (originalPitch > 180f) originalPitch -= 360f;

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            float pitch = Mathf.Lerp(originalPitch, downAngle, elapsed);
            transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            elapsed += Time.deltaTime * speed;
            yield return null;
        }

        transform.localRotation = Quaternion.Euler(downAngle, 0f, 0f);
        yield return new WaitForSeconds(0.5f);

        elapsed = 0f;
        while (elapsed < 1f)
        {
            float pitch = Mathf.Lerp(downAngle, originalPitch, elapsed);
            transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            elapsed += Time.deltaTime * speed;
            yield return null;
        }

        transform.localRotation = Quaternion.Euler(originalPitch, 0f, 0f);
        IsAnimating = false;
        isLookingDown = false;

        var playerController = GetComponentInParent<PlayerController>();
        if (playerController != null)
            playerController.SyncPitchFromCamera();
    }
}
