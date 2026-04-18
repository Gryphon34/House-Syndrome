using UnityEngine;
using TMPro;
using System.Collections;

public class LoadingDotsAnimation : MonoBehaviour
{
public TMP_Text textComponent;
public string baseText = "Loading";
public float speed = 0.5f;

void OnEnable()
{
    if (textComponent == null) textComponent = GetComponent<TMP_Text>();
    StartCoroutine(AnimateDots());
}

IEnumerator AnimateDots()
{
    int dotCount = 1;
    while (true)
    {
        // 점을 1개에서 3개까지 반복합니다.
        string dots = new string('.', dotCount);
        textComponent.text = baseText + dots;

        dotCount++;
        if (dotCount > 3) dotCount = 1;

        yield return new WaitForSeconds(speed);
    }
}
}