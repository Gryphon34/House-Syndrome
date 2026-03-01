using UnityEngine;

public class MirrorTest : MonoBehaviour
{
    public Transform player;
    public Transform mirror;

    private void Update()
    {
        // 반사 카메라 위치: 거울 기준으로 플레이어 위치 반전
        Vector3 localPlayer = mirror.InverseTransformPoint(player.position);
        transform.position = mirror.TransformPoint(new Vector3(localPlayer.x, localPlayer.y, -localPlayer.z));

        // 반사 카메라가 플레이어를 바라보게 함
        transform.LookAt(player.position);
    }
}
