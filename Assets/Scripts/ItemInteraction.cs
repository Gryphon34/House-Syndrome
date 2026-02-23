using System.Collections;
using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ItemInteraction : MonoBehaviour
{
    public float interactDistance = 3f;
    [Tooltip("WalkingPlayer ?? ???. ?? ??? 'WalkingPlayer' ???? ??")]
    public Camera walkingCamera;
    public GameObject interactPromptUI; // "??????? (E)" ????
    public TextMeshProUGUI logText;     // ?????? ??? ?? ????? ????

    [Header("Inventory")]
    public List<string> collectedItems = new List<string>();

    void Update()
    {
        // ???????? ????? ??? ?? ??
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

        if (!Physics.Raycast(ray, out hit, interactDistance))
        {
            interactPromptUI.SetActive(false);
            return;
        }

        // 1) ???(phone ??) - ???? Item? ?? (??? phone? ?? PhonePlace?? ??)
        Item item = hit.transform.GetComponent<Item>();
        if (item != null && item.enabled)
        {
            interactPromptUI.SetActive(true);
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (item.itemName == PhonePlace.PhoneItemName)
                    HidePhone(item);
                else
                    Collect(item);
            }
            return;
        }

        // 2) phone? ?? ??(PhonePlace) ?? ? E? ?? ???
        PhonePlace phonePlace = hit.transform.GetComponent<PhonePlace>();
        if (phonePlace != null)
        {
            interactPromptUI.SetActive(true);
            if (Input.GetKeyDown(KeyCode.E))
                ShowPhone(phonePlace);
            return;
        }

        interactPromptUI.SetActive(false);
    }

    /// <summary>E키 상호작용 후에도 씬에 남겨둘 아이템 이름 (사라지지 않음)</summary>
    public static readonly string[] PersistentItemNames = { "bathroom_handle" };

    void Collect(Item item)
    {
        collectedItems.Add(item.itemName);

        if (logText != null)
        {
            logText.text = $"'{item.itemName}'??(??) ??????.\n{item.description}";
            Invoke("ClearLog", 4f); // 4?? ?? ??? ????
        }

        bool keepInScene = false;
        for (int i = 0; i < PersistentItemNames.Length; i++)
        {
            if (item.itemName == PersistentItemNames[i])
            {
                keepInScene = true;
                break;
            }
        }
        if (!keepInScene)
            Destroy(item.gameObject);
    }

    [Header("Phone - E? ??? 10? ? ?? ??")]
    public float phoneCapsuleDelay = 10f;
    [Tooltip("????? ? ?? ????. E? phone ?? ? 10? ? ????")]
    public GameObject capsuleToShowAfterPhone;

    Coroutine _phoneCapsuleRoutine;

    void HidePhone(Item phoneItem)
    {
        GameObject go = phoneItem.gameObject;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            r.enabled = false;
        phoneItem.enabled = false;
        go.GetComponent<PhonePlace>().enabled = true;

        if (capsuleToShowAfterPhone != null)
        {
            if (_phoneCapsuleRoutine != null)
                StopCoroutine(_phoneCapsuleRoutine);
            _phoneCapsuleRoutine = StartCoroutine(ShowCapsuleAfterDelay());
        }
    }

    IEnumerator ShowCapsuleAfterDelay()
    {
        yield return new WaitForSeconds(phoneCapsuleDelay);
        if (capsuleToShowAfterPhone != null)
            capsuleToShowAfterPhone.SetActive(true);
        _phoneCapsuleRoutine = null;
    }

    void ShowPhone(PhonePlace phonePlace)
    {
        GameObject go = phonePlace.gameObject;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            r.enabled = true;
        go.GetComponent<Item>().enabled = true;
        phonePlace.enabled = false;
    }

    void ClearLog() { if (logText != null) logText.text = ""; }
}