using UnityEngine;

/// <summary>
/// 잠자기 전 규칙: 밤 + bathroom_handle으로 오브젝트 켜기 → 다시 E로 끄기 를 완료해야만 잠자기 가능.
/// </summary>
public class SleepRuleManager : MonoBehaviour
{
    public static SleepRuleManager Instance { get; private set; }

    /// <summary> 이번 밤에 handle으로 오브젝트를 한 번이라도 켰는지 </summary>
    bool handleShownThisNight;
    /// <summary> 켠 뒤 다시 끈 적 있는지 </summary>
    bool handleHiddenAfterShow;
    /// <summary> bathroom_handle을 E키로 한 번이라도 상호작용한 적 있는지 (리셋 안 함, CameraLookDown 등에서 사용) </summary>
    bool hasEverUsedBathroomHandle;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary> bathroom_handle E키 상호작용 시 한 번 호출됨. (CameraLookDown 등에서 구독) </summary>
    public static System.Action OnBathroomHandleUsed;

    /// <summary> bathroom_handle E키 토글 시 호출. 켰으면 true, 껐으면 false (토글 후 상태). </summary>
    public void RecordBathroomHandleToggle(bool isNowActive)
    {
        hasEverUsedBathroomHandle = true;
        if (isNowActive)
            handleShownThisNight = true;
        else if (handleShownThisNight)
            handleHiddenAfterShow = true;
        OnBathroomHandleUsed?.Invoke();
    }

    /// <summary> bathroom_handle을 E키로 한 번이라도 상호작용한 적 있으면 true. </summary>
    public bool HasEverUsedBathroomHandle => hasEverUsedBathroomHandle;

    /// <summary> 새 날이 시작될 때 규칙 상태 리셋. SpawnManager에서 호출. </summary>
    public void ResetRule()
    {
        handleShownThisNight = false;
        handleHiddenAfterShow = false;
    }

    /// <summary> 밤이고, handle 켜기 → 끄기 순서를 완료했을 때만 true. </summary>
    public bool CanSleep =>
        (SpawnManager.Instance != null && SpawnManager.Instance.IsNightTime)
        && handleShownThisNight
        && handleHiddenAfterShow;
}
