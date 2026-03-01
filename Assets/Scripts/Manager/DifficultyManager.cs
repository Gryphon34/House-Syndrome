using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    // 어디서든 접근할 수 있게 싱글톤(Singleton) 구조로 만듭니다.
    public static DifficultyManager Instance;

    [Header("Game Progress")]
    public int currentDay = 1; // 1일부터 7일까지

    [Header("Difficulty Settings (Day 1 -> Day 7)")]
    public float minDecay = 0.2f;
    public float maxDecay = 2.5f;

    public float minGain = 20f;
    public float maxGain = 10f;

    public float minPenalty = 2f;
    public float maxPenalty = 15f;

    public int minSeqLength = 3;
    public int maxSeqLength = 6;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // [수정] 인스턴스가 확정되었을 때만 파괴 방지 설정
        }
        else
        {
            Destroy(gameObject); // 이미 존재한다면 새로 생성된 것은 즉시 삭제
            return;
        }
    }

    // 현재 날짜에 따른 난이도 비율 (0 ~ 1) 계산
    private float GetDifficultyT()
    {
        return Mathf.Clamp01((currentDay - 1) / 6f);
    }

    // --- 외부(HandInputSystem)에서 가져갈 수치들 ---

    public float GetConstantDecayRate()
    {
        return Mathf.Lerp(minDecay, maxDecay, GetDifficultyT());
    }

    public float GetCycleIncreaseAmount()
    {
        return Mathf.Lerp(minGain, maxGain, GetDifficultyT());
    }

    public float GetFailPenaltyAmount()
    {
        return Mathf.Lerp(minPenalty, maxPenalty, GetDifficultyT());
    }

    public int GetSequenceLength()
    {
        return Mathf.RoundToInt(Mathf.Lerp(minSeqLength, maxSeqLength, GetDifficultyT()));
    }

    public void NextDay()
    {
        currentDay++;
        if (currentDay > 7) Debug.Log("모든 날짜 클리어!");
    }
}