using System.Collections;
using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
                else if (item.itemName == "box")
                    InteractWithBox(item);
                else if (IsHandleItem(item.itemName))
                {
                    if (!_hasDoneBathroomHandleFadeOnce)
                        StartCoroutine(ToggleHandleObjectWithFade());
                    else
                        ToggleHandleObject();
                }
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
    public static readonly string[] PersistentItemNames = { "bathroom_handle"};

    /// <summary>이름이 handle(또는 bathroom_handle)인 아이템은 E키로 지정 오브젝트 활성/비활성 토글.</summary>
    public const string BathroomHandleItemName = "bathroom_handle";

    [Header("Box - E키 상호작용 시 box 사라지고 box_glitch 활성화")]
    [Tooltip("box 아이템과 E키 상호작용 시 활성화할 오브젝트 (box_glitch)")]
    public GameObject boxGlitchObject;

    private GameObject _interactedBoxObject;

    [Header("Handle - E키로 표시/숨김 토글")]
    [Tooltip("이름이 handle인 아이템과 E키 상호작용 시 켜졌다 꺼졌다 할 오브젝트들 (복제한 prefab 인스턴스 등 모두 추가)")]
    public List<GameObject> objectsToToggleWithHandle = new List<GameObject>();

    [Header("Handle - 화면 페이드 (bathroom_handle E키 시)")]
    [Tooltip("bathroom_handle 상호작용 시 까매졌다 풀리는 효과에 쓸 풀스크린 검정 Image. BedInteraction의 fadeImage와 동일 오브젝트 지정 가능.")]
    public Image bathroomHandleFadeImage;
    [Tooltip("화면이 검게 유지되는 시간(초)")]
    public float bathroomHandleFadeHoldDuration = 3f;
    [Tooltip("페이드 인/아웃에 걸리는 시간(초)")]
    public float bathroomHandleFadeTransitionDuration = 0.5f;

    private bool _isHandleFading = false;
    private bool _hasDoneBathroomHandleFadeOnce = false;

    IEnumerator ToggleHandleObjectWithFade()
    {
        if (_isHandleFading || bathroomHandleFadeImage == null)
        {
            ToggleHandleObject();
            yield break;
        }
        _isHandleFading = true;
        bathroomHandleFadeImage.gameObject.SetActive(true);
        Color c = bathroomHandleFadeImage.color;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / bathroomHandleFadeTransitionDuration;
            c.a = Mathf.Clamp01(t);
            bathroomHandleFadeImage.color = c;
            yield return null;
        }
        c.a = 1f;
        bathroomHandleFadeImage.color = c;
        ToggleHandleObject();
        yield return new WaitForSeconds(bathroomHandleFadeHoldDuration);
        t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime / bathroomHandleFadeTransitionDuration;
            c.a = Mathf.Clamp01(t);
            bathroomHandleFadeImage.color = c;
            yield return null;
        }
        c.a = 0f;
        bathroomHandleFadeImage.color = c;
        bathroomHandleFadeImage.gameObject.SetActive(false);
        _isHandleFading = false;
        _hasDoneBathroomHandleFadeOnce = true;
    }

    static bool IsHandleItem(string itemName)
    {
        return itemName == BathroomHandleItemName;
    }

    void ToggleHandleObject()
    {
        if (objectsToToggleWithHandle == null || objectsToToggleWithHandle.Count == 0) return;
        bool setActive = true;
        for (int i = 0; i < objectsToToggleWithHandle.Count; i++)
        {
            if (objectsToToggleWithHandle[i] != null)
            {
                setActive = !objectsToToggleWithHandle[i].activeSelf;
                break;
            }
        }
        for (int i = 0; i < objectsToToggleWithHandle.Count; i++)
        {
            if (objectsToToggleWithHandle[i] != null)
                objectsToToggleWithHandle[i].SetActive(setActive);
        }

        if (SleepRuleManager.Instance != null)
            SleepRuleManager.Instance.RecordBathroomHandleToggle(setActive);
    }

    void InteractWithBox(Item item)
    {
        _interactedBoxObject = item.gameObject;
        _interactedBoxObject.SetActive(false);
        if (boxGlitchObject != null)
            boxGlitchObject.SetActive(true);
    }

    void OnEnable()
    {
        SpawnManager.OnDayChangedEvent += ResetBoxState;
    }

    void OnDisable()
    {
        SpawnManager.OnDayChangedEvent -= ResetBoxState;
    }

    void ResetBoxState()
    {
        if (_interactedBoxObject != null)
        {
            _interactedBoxObject.SetActive(true);
            _interactedBoxObject = null;
        }
        if (boxGlitchObject != null)
            boxGlitchObject.SetActive(false);
    }

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