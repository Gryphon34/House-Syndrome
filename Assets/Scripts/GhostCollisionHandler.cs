using UnityEngine;

public class GhostCollisionHandler : MonoBehaviour
{
    [Header("Settings")]
    public string ghostTag = "Ghost"; // 귀신 오브젝트의 태그

    private void OnTriggerEnter(Collider other)
    {
        // 1. 부딪힌 오브젝트가 귀신인지 확인
        if (other.CompareTag(ghostTag))
        {
            Debug.Log("<color=red>귀신에게 붙잡혔습니다! 전날로 돌아갑니다.</color>");

            // 2. SpawnManager를 통해 실패 로직 실행
            if (SpawnManager.Instance != null)
            {
                SpawnManager.Instance.ReturnToPreviousDay();
            }
        }
    }
}