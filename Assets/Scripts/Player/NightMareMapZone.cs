using UnityEngine;

/// <summary>
/// NightMarePlayer가 NightMareMap 영역에 들어오면 양손 UI를 동시에 표시합니다.
/// NightMareMap 루트에 붙이면 자식 렌더러 기준으로 플레이 영역을 자동 계산합니다.
/// </summary>
public class NightMareMapZone : MonoBehaviour
{
    [Tooltip("비우면 이름이 NightMarePlayer인 오브젝트를 찾습니다.")]
    public string nightmarePlayerName = "NightMarePlayer";

    [Tooltip("자동 계산된 영역에 더할 여유(미터). 침대·플레이어 스폰 위치 포함용.")]
    public float boundsPadding = 8f;

    [Tooltip("false면 씬에 설정한 BoxCollider 범위를 그대로 사용합니다.")]
    public bool autoFitBounds = true;

    Bounds _playBounds;
    bool _boundsReady;

    void Awake()
    {
        var col = GetComponent<BoxCollider>();
        if (col != null)
            col.isTrigger = true;
    }

    void Start()
    {
        RebuildPlayBounds();
    }

    void Update()
    {
        if (!_boundsReady)
            RebuildPlayBounds();

        HandInputSystem.SetNightMareMapMode(IsNightMarePlayerInsideZone());
    }

    void RebuildPlayBounds()
    {
        if (autoFitBounds)
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                _playBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                        _playBounds.Encapsulate(renderers[i].bounds);
                }
                _playBounds.Expand(boundsPadding);
                _boundsReady = true;
                return;
            }
        }

        var col = GetComponent<Collider>();
        if (col != null)
        {
            _playBounds = col.bounds;
            _playBounds.Expand(boundsPadding);
            _boundsReady = true;
        }
    }

    bool IsNightMarePlayerInsideZone()
    {
        var player = GameObject.Find(nightmarePlayerName);
        if (player == null || !player.activeInHierarchy)
            return false;

        if (!_boundsReady)
            return false;

        Vector3 checkPos = GetNightmareCheckPosition(player);
        return _playBounds.Contains(checkPos);
    }

    static Vector3 GetNightmareCheckPosition(GameObject player)
    {
        Camera cam = Camera.main;
        if (cam != null && cam.gameObject.activeInHierarchy
            && cam.transform.IsChildOf(player.transform))
        {
            return cam.transform.position;
        }

        return player.transform.position;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!_boundsReady)
            RebuildPlayBounds();
        if (!_boundsReady)
            return;

        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.25f);
        Gizmos.DrawCube(_playBounds.center, _playBounds.size);
    }
#endif
}
