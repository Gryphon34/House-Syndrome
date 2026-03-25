using UnityEngine;

/// <summary>
/// 현재 일차에 해당하는 맵만 활성화하고 나머지는 비활성화합니다.
/// </summary>
public class DayMapManager : MonoBehaviour
{
    [Header("Day Maps (1일차 = index 0, 2일차 = index 1, ...)")]
    [Tooltip("각 일차별 맵 루트 오브젝트. 비어 있으면 해당 일차는 무시.")]
    public GameObject[] dayMaps = new GameObject[8];

    void Start()
    {
        RefreshMapsForCurrentDay();
    }

    /// <summary>
    /// 현재 일차에 맞는 맵만 켜고 나머지는 끕니다. SpawnManager에서 날짜 변경 시 호출.
    /// </summary>
    public void RefreshMapsForCurrentDay()
    {
        int day = GetRawCurrentDay();
        int maxIndex = dayMaps.Length > 0 ? dayMaps.Length - 1 : 0;
        int index = Mathf.Clamp(day - 1, 0, maxIndex);

        for (int i = 0; i < dayMaps.Length; i++)
        {
            if (dayMaps[i] != null)
                dayMaps[i].SetActive(false);
        }

        if (index >= 0 && index < dayMaps.Length && dayMaps[index] != null)
            dayMaps[index].SetActive(true);
    }

    static int GetRawCurrentDay()
    {
        if (DifficultyManager.Instance != null)
            return Mathf.Max(1, DifficultyManager.Instance.currentDay);
        if (SpawnManager.Instance != null)
            return Mathf.Max(1, SpawnManager.Instance.GetCurrentDay());
        return 1;
    }
}
