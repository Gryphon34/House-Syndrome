using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


public enum GhostType

{

    Normal,

    Clock,

    Mimic,

    Reflection,

    Stalker,

    Twin

}
[System.Serializable]
public struct GhostFaceMapping
{
    public GhostType type;
    public Sprite faceSprite;
}

public class JumpScareManager : MonoBehaviour
{
    public static JumpScareManager Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject jumpScareCanvas; // 점프스케어용 캔버스 (우선순위 100 설정)
    public Image ghostFaceImage;       // 귀신 이미지가 들어갈 Image 컴포넌트

    [Header("Audio")]
    public AudioSource scareAudioSource;
    public AudioClip scareScreamClip;

    [Header("Ghost Faces")]
    public List<GhostFaceMapping> faceMappings;

    [Header("Settings")]
    public float scareDuration = 2.0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (jumpScareCanvas != null) jumpScareCanvas.SetActive(false);
    }

    public void TriggerJumpScare(GhostType ghostType)
    {
        if (jumpScareCanvas == null || jumpScareCanvas.activeSelf) return;

        // 1. 귀신 타입에 맞는 스프라이트 찾기
        Sprite targetSprite = null;
        foreach (var mapping in faceMappings)
        {
            if (mapping.type == ghostType)
            {
                targetSprite = mapping.faceSprite;
                break;
            }
        }

        if (targetSprite != null)
        {
            ghostFaceImage.sprite = targetSprite;
            
            // 2. 캔버스 활성화 및 소리 재생
            jumpScareCanvas.SetActive(true);
            if (scareAudioSource != null && scareScreamClip != null)
            {
                scareAudioSource.PlayOneShot(scareScreamClip);
            }

            // 3. 연출 및 날짜 이동 시작
            StartCoroutine(PopOutEffect());
            StartCoroutine(HandleReturnDayAfterScare());
        }
    }

    private IEnumerator PopOutEffect()
    {
        // 이미지가 중앙에서 확 커지는 연출
        ghostFaceImage.rectTransform.localScale = Vector3.one * 0.2f;
        float elapsed = 0f;
        float duration = 0.15f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            ghostFaceImage.rectTransform.localScale = Vector3.Lerp(Vector3.one * 0.2f, Vector3.one, elapsed / duration);
            yield return null;
        }
        ghostFaceImage.rectTransform.localScale = Vector3.one;
    }

    private IEnumerator HandleReturnDayAfterScare()
    {
        yield return new WaitForSeconds(scareDuration);
        
        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.ReturnToPreviousDay();
        }
    }
}