using UnityEngine;

/// <summary>
/// 현재 일차에 해당하는 맵만 활성화하고 나머지는 비활성화합니다.
/// </summary>
public class DayMapManager : MonoBehaviour
{
    [Header("Day Maps (1일차 = index 0, 2일차 = index 1, ...)")]
    [Tooltip("각 일차별 맵 루트 오브젝트. 비어 있으면 해당 일차는 무시.")]
    public GameObject[] dayMaps = new GameObject[8];

    [Header("NightMare Map")]
    [Tooltip("악몽 맵 루트 오브젝트.")]
    public GameObject nightmareMap;

    [Header("Ending Maps (8일차 분기)")]
    [Tooltip("진엔딩 맵 루트 오브젝트. 게임 시작 시 비활성화됩니다.")]
    public GameObject trueEndingMap;
    [Tooltip("배드엔딩 맵 루트 오브젝트. 게임 시작 시 비활성화됩니다.")]
    public GameObject badEndingMap;

    // NightMare 진입 전 활성화 되어있던 데이맵 저장용
    private GameObject _activeMapBeforeNightmare;

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

        // 데이맵 전환 시 NightMareMap / EndingMap은 항상 비활성화
        if (nightmareMap != null)
            nightmareMap.SetActive(false);
        if (trueEndingMap != null)
            trueEndingMap.SetActive(false);
        if (badEndingMap != null)
            badEndingMap.SetActive(false);
    }

    /// <summary>
    /// NightMareMap 진입 시 호출.
    /// 현재 활성 데이맵을 저장하고 비활성화, NightMareMap을 활성화합니다.
    /// </summary>
    public void EnterNightmareMap()
    {
        // 현재 켜져있는 데이맵 저장
        _activeMapBeforeNightmare = null;
        for (int i = 0; i < dayMaps.Length; i++)
        {
            if (dayMaps[i] != null && dayMaps[i].activeSelf)
            {
                _activeMapBeforeNightmare = dayMaps[i];
                break;
            }
        }

        // 모든 데이맵 비활성화
        for (int i = 0; i < dayMaps.Length; i++)
        {
            if (dayMaps[i] != null)
                dayMaps[i].SetActive(false);
        }

        // NightMareMap 활성화
        if (nightmareMap != null)
            nightmareMap.SetActive(true);
    }

    /// <summary>
    /// NightMareMap 탈출 시 호출.
    /// NightMareMap을 비활성화하고 진입 전 데이맵을 복원합니다.
    /// </summary>
    public void ExitNightmareMap()
    {
        // NightMareMap 비활성화
        if (nightmareMap != null)
            nightmareMap.SetActive(false);

        // 이전 데이맵 복원, 없으면 현재 날짜 기준으로 재계산
        if (_activeMapBeforeNightmare != null)
            _activeMapBeforeNightmare.SetActive(true);
        else
            RefreshMapsForCurrentDay();

        _activeMapBeforeNightmare = null;
    }

    /// <summary>
    /// 진엔딩 진입 시 호출.
    /// 모든 데이맵·NightMareMap을 비활성화하고 TrueEndingMap을 활성화합니다.
    /// </summary>
    public void EnterTrueEndingMap()
    {
        DeactivateAllMaps();
        if (trueEndingMap != null)
            trueEndingMap.SetActive(true);
    }

    /// <summary>
    /// 배드엔딩 진입 시 호출.
    /// 모든 데이맵·NightMareMap을 비활성화하고 BadEndingMap을 활성화합니다.
    /// </summary>
    public void EnterBadEndingMap()
    {
        DeactivateAllMaps();
        if (badEndingMap != null)
            badEndingMap.SetActive(true);
    }

    /// <summary>
    /// 데이맵·NightMareMap·EndingMap을 모두 비활성화하는 내부 유틸.
    /// </summary>
    private void DeactivateAllMaps()
    {
        for (int i = 0; i < dayMaps.Length; i++)
            if (dayMaps[i] != null) dayMaps[i].SetActive(false);
        if (nightmareMap != null) nightmareMap.SetActive(false);
        if (trueEndingMap != null) trueEndingMap.SetActive(false);
        if (badEndingMap != null) badEndingMap.SetActive(false);
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
