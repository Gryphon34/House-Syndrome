using TMPro;
using UnityEngine;

public class DialogueOnInteract : MonoBehaviour
{
    [Header("Dialogue")]
    [TextArea(2, 6)]
    public string message;

    [Tooltip("0 이하면 DialogueUI의 기본 시간을 사용합니다.")]
    public float durationSeconds = 0f;

    [Tooltip("체크하면 대사가 표시된 뒤 ItemInteraction의 기본 동작(줍기/열기 등)을 막습니다.")]
    public bool consumeInteraction = false;

    public bool TryInteract()
    {
        var ui = DialogueUI.EnsureInstance();
        if (ui == null)
        {
            Debug.LogWarning($"[DialogueOnInteract] DialogueUI.Instance가 씬에 없습니다. ({name})");
            return consumeInteraction;
        }

        if (durationSeconds > 0f) ui.Show(message, durationSeconds);
        else ui.Show(message);

        return consumeInteraction;
    }
}

