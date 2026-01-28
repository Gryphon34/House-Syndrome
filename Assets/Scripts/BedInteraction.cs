using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BedInteraction : MonoBehaviour
{
    [Header("Player Objects")]
    public GameObject walkingPlayer;
    public GameObject nightmarePlayer;

    [Header("Cameras")]
    public Camera walkingCamera;    // 탐사용 카메라를 직접 연결하세요.
    public Camera nightmareCamera;  // 가위눌림용 카메라를 직접 연결하세요.

    [Header("UI & Effect")]
    public Image fadeImage;
    public GameObject sleepPromptUI;

    [Header("Settings")]
    public float interactDistance = 3f;
    public string bedTag = "Bed";

    private bool isTransitioning = false;

    public GameObject nightmareHUD; // 날짜와 게이지가 들어있는 부모 오브젝트

    void Start()
    {
        if (walkingPlayer != null) walkingPlayer.SetActive(true);
        if (nightmarePlayer != null) nightmarePlayer.SetActive(false);
        if (nightmareHUD != null) nightmareHUD.SetActive(false); // 시작할 때 HUD 끔
        if (fadeImage != null) fadeImage.color = new Color(0, 0, 0, 0);
    }

    void Update()
    {
        // 이미 가위눌림 모드이거나 전환 중이면 체크 안 함
        if (isTransitioning || (nightmarePlayer != null && nightmarePlayer.activeSelf)) return;

        CheckBed();
    }

    void CheckBed()
    {
        if (walkingCamera == null) return;

        // 활성화된 탐사용 카메라 기준으로 레이 발사
        Ray ray = walkingCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            if (hit.transform.CompareTag(bedTag))
            {
                if (sleepPromptUI != null) sleepPromptUI.SetActive(true);
                if (Input.GetKeyDown(KeyCode.E)) StartCoroutine(SwapToNightmare());
                return;
            }
        }
        if (sleepPromptUI != null) sleepPromptUI.SetActive(false);
    }

    IEnumerator SwapToNightmare()
    {
        isTransitioning = true;
        if (sleepPromptUI != null) sleepPromptUI.SetActive(false);

        // 1. 암전 (Fade Out)
        float timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime;
            if (fadeImage != null) fadeImage.color = new Color(0, 0, 0, timer);
            yield return null;
        }

        walkingPlayer.SetActive(false);
        nightmarePlayer.SetActive(true);
        if (nightmareHUD != null) nightmareHUD.SetActive(true); // 가위눌림 시작 시 HUD 켬

        // 씬 전환 시간 벌기
        yield return new WaitForSeconds(1f);

        // 3. 다시 밝아짐 (Fade In)
        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            if (fadeImage != null) fadeImage.color = new Color(0, 0, 0, timer);
            yield return null;
        }
        isTransitioning = false;
    }
}