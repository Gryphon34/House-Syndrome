using UnityEngine;
using TMPro;

public class ScriptManager : MonoBehaviour
{
    [SerializeField]
    private ChatData chatData;

    [SerializeField]
    private TMP_Text dialogueText;

    [SerializeField]
    private int currentIndex = 0;

    private void Start()
    {
        if (chatData == null)
        {
            Debug.LogError("ChatData가 연결되지 않았습니다.");
            return;
        }

        if (dialogueText == null)
        {
            Debug.LogError("Dialogue Text가 연결되지 않았습니다.");
            return;
        }

        ShowDialogue(currentIndex);
    }

    public void ShowDialogue(int index)
    {
        if (chatData.chatDataList == null || chatData.chatDataList.Count == 0)
        {
            Debug.LogError("ChatData에 대사 데이터가 없습니다.");
            return;
        }

        if (index < 0 || index >= chatData.chatDataList.Count)
        {
            Debug.LogError($"잘못된 대사 인덱스입니다: {index}");
            return;
        }

        Chat chat = chatData.chatDataList[index];
        dialogueText.text = chat.content;

        Debug.Log($"ID: {chat.id}, Content: {chat.content}");
    }

    public void ShowNextDialogue()
    {
        currentIndex++;

        if (currentIndex >= chatData.chatDataList.Count)
        {
            currentIndex = chatData.chatDataList.Count - 1;
            return;
        }

        ShowDialogue(currentIndex);
    }
}