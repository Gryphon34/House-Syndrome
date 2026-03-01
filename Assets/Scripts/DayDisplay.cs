using TMPro;
using UnityEngine;

public class DayDisplay : MonoBehaviour
{
    void Update()
    {
        GetComponent<TextMeshProUGUI>().text = "Day " + DifficultyManager.Instance.currentDay;
    }
}
