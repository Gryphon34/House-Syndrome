using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Voice Recorder UI의 사진(Image)에 붙여 두면 클릭 시 ItemInteraction에서 지정한 클립을 재생합니다.
/// Image의 Raycast Target이 켜져 있어야 하며, Canvas에 GraphicRaycaster와 씬에 EventSystem이 필요합니다.
/// </summary>
[RequireComponent(typeof(Image))]
public class VoiceRecorderPhotoClick : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("비우면 씬에서 ItemInteraction을 자동 탐색합니다.")]
    public ItemInteraction itemInteraction;

    void Awake()
    {
        if (itemInteraction == null)
            itemInteraction = FindFirstObjectByType<ItemInteraction>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (itemInteraction != null)
            itemInteraction.OnVoiceRecorderPhotoClicked();
    }
}
