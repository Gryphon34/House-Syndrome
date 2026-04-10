using UnityEngine;
using UnityEngine.Playables;

public class CutsceneTrigger : MonoBehaviour
{
    public PlayableDirector director;
    private bool hasPlayed = false;

    private void OnTriggerEnter(Collider other)
    {
        // 큐브에 닿은 것이 플레이어 태그를 가졌는지 확인
        if (other.CompareTag("Player") && !hasPlayed)
        {
            PlayCutscene();
        }
    }

    void PlayCutscene()
    {
        hasPlayed = true;
        if (director != null)
        {
            director.Play();
        }
    }
}