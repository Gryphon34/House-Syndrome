using UnityEngine;

/// <summary>
/// 애니메이션(또는 Animator가 재생하는 클립)이 끝난 뒤 이 오브젝트와 추가 지정 오브젝트를 끄거나 파괴합니다.
/// Animation 창에서 클립 마지막 프레임에 이벤트를 추가하고, 함수로 <see cref="OnAnimationEnd"/> 를 선택하세요.
/// </summary>
public class HideWhenAnimationEnds : MonoBehaviour
{
	public enum AfterAnimation
	{
		DisableGameObject,
		DestroyGameObject
	}

	[System.Serializable]
	public struct ExtraTarget
	{
		public GameObject target;
		[Tooltip("비활성화할지, 파괴할지")]
		public AfterAnimation action;
	}

	[Tooltip("애니메이션 종료 후 이 컴포넌트가 붙은 오브젝트를 비활성화할지, 파괴할지")]
	[SerializeField] AfterAnimation afterAnimation = AfterAnimation.DisableGameObject;

	[Tooltip("같은 시점에 함께 없앨 다른 오브젝트들 (비어 있으면 이 오브젝트만 처리)")]
	[SerializeField] ExtraTarget[] extraTargets;

	/// <summary>
	/// Animation / Animator 클립에 추가한 이벤트에서 호출할 함수 이름입니다.
	/// </summary>
	public void OnAnimationEnd()
	{
		if (extraTargets != null)
		{
			for (int i = 0; i < extraTargets.Length; i++)
			{
				GameObject go = extraTargets[i].target;
				if (go == null) continue;
				ApplyTo(go, extraTargets[i].action);
			}
		}

		ApplyTo(gameObject, afterAnimation);
	}

	static void ApplyTo(GameObject go, AfterAnimation action)
	{
		if (go == null) return;
		if (action == AfterAnimation.DestroyGameObject)
			Object.Destroy(go);
		else
			go.SetActive(false);
	}
}
