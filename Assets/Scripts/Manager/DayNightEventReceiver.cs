using UnityEngine;

/// <summary>
/// Directional Light (또는 애니메이션이 있는 오브젝트)에 붙여서
/// Animation Event를 DayManager로 전달하는 스크립트
/// </summary>
[RequireComponent(typeof(Animator))]
public class DayNightEventReceiver : MonoBehaviour
{
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Animation Event에서 호출됨 - 밤이 되었을 때
    /// </summary>
    public void OnNightReached()
    {
        Debug.Log("<color=magenta>[DayNightEventReceiver] OnNightReached 호출됨!</color>");
        
        // 이 오브젝트의 애니메이터 직접 멈춤
        if (animator != null)
        {
            animator.speed = 0f;
            Debug.Log("<color=magenta>[DayNightEventReceiver] Animator 멈춤</color>");
        }

        // DayManager에 알림
        if (DayManager.Instance != null)
        {
            DayManager.Instance.OnNightReached();
        }
        else
        {
            Debug.LogWarning("DayNightEventReceiver: DayManager.Instance를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 애니메이션 다시 시작 (DayManager에서 호출)
    /// </summary>
    public void RestartAnimation()
    {
        if (animator != null)
        {
            animator.speed = 1f;
            animator.Play(0, 0, 0f); // 처음부터 재생
            Debug.Log("<color=magenta>[DayNightEventReceiver] Animator 재시작</color>");
        }
    }
}
