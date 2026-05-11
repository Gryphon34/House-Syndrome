using UnityEngine;

/// <summary>
/// 개발용 임시. 숫자키 1~9 누르면 해당 일차로 이동. 빌드 시 제거하거나 비활성화 권장.
/// </summary>
public class DevDaySkip : MonoBehaviour
{
    void Update()
    {
        if (SpawnManager.Instance == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) SpawnManager.Instance.GoToDay(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SpawnManager.Instance.GoToDay(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SpawnManager.Instance.GoToDay(3);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SpawnManager.Instance.GoToDay(4);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SpawnManager.Instance.GoToDay(5);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SpawnManager.Instance.GoToDay(6);
        if (Input.GetKeyDown(KeyCode.Alpha7)) SpawnManager.Instance.GoToDay(7);
        if (Input.GetKeyDown(KeyCode.Alpha8)) SpawnManager.Instance.GoToDay(8);
        if (Input.GetKeyDown(KeyCode.Alpha9)) SpawnManager.Instance.GoToDay(9);
    }
}
