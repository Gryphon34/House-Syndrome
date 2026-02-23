using UnityEngine;

/// <summary>
/// [사용 중단] 낮/밤은 DayNightCycle이 담당합니다.
/// 씬에 이 컴포넌트가 남아 있어도 동작에는 영향 없으며, 필요 없으면 제거해도 됩니다.
/// </summary>
[RequireComponent(typeof(Animator))]
public class DayNightEventReceiver : MonoBehaviour
{
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    /// <summary> 애니메이션 이벤트용. 밤 여부는 이제 DayNightCycle.IsNightTime을 사용하세요. </summary>
    public void OnNightReached()
    {
        if (animator != null)
            animator.speed = 0f;
    }

    /// <summary> DayManager는 더 이상 호출하지 않음. 낮 리셋은 DayNightCycle.ResetToDay() 사용. </summary>
    public void RestartAnimation()
    {
        if (animator != null)
        {
            animator.speed = 1f;
            animator.Play(0, 0, 0f);
        }
    }
}
