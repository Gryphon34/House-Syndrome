using TMPro;
using UnityEngine;

public class DayDisplay : MonoBehaviour
{
    private TextMeshProUGUI _textMesh;

    void Start()
    {
        // 처음에 한 번만 컴포넌트를 찾아 메모리에 저장합니다.
        _textMesh = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (_textMesh != null && DifficultyManager.Instance != null)
        {
            // 실시간으로 날짜 정보를 업데이트합니다.
            _textMesh.text = "Day " + DifficultyManager.Instance.currentDay;
        }
    }
}