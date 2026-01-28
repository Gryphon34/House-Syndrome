using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ItemInteraction : MonoBehaviour
{
    public float interactDistance = 3f;
    public Camera walkingCamera;
    public GameObject interactPromptUI; // "조사하기 (E)" 텍스트
    public TextMeshProUGUI logText;     // 아이템 획득 시 띄워줄 알림창

    [Header("Inventory")]
    public List<string> collectedItems = new List<string>();

    void Update()
    {
        // 가위눌림 중에는 작동 안 함
        if (DifficultyManager.Instance == null || walkingCamera == null || !walkingCamera.gameObject.activeInHierarchy)
        {
            interactPromptUI.SetActive(false);
            return;
        }

        CheckItem();
    }

    void CheckItem()
    {
        Ray ray = walkingCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        // "Item" 레이어를 가진 물체만 감지하도록 설정하는 것이 좋습니다.
        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            Item item = hit.transform.GetComponent<Item>();
            if (item != null)
            {
                interactPromptUI.SetActive(true);
                if (Input.GetKeyDown(KeyCode.E))
                {
                    Collect(item);
                }
                return;
            }
        }
        interactPromptUI.SetActive(false);
    }

    void Collect(Item item)
    {
        collectedItems.Add(item.itemName);

        if (logText != null)
        {
            logText.text = $"'{item.itemName}'을(를) 발견했다.\n{item.description}";
            Invoke("ClearLog", 4f); // 4초 후 로그 삭제
        }
        Destroy(item.gameObject); // 월드에서 아이템 제거
    }

    void ClearLog() { if (logText != null) logText.text = ""; }
}