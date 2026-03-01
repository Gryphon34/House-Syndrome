using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    // ��𼭵� ������ �� �ְ� �̱���(Singleton) ������ ����ϴ�.
    public static DifficultyManager Instance;

    [Header("Game Progress")]
    public int currentDay = 1; // 1�Ϻ��� 7�ϱ���

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
            DontDestroyOnLoad(gameObject); // [����] �ν��Ͻ��� Ȯ���Ǿ��� ���� �ı� ���� ����
        }
        else
        {
            Destroy(gameObject); // �̹� �����Ѵٸ� ���� ������ ���� ��� ����
            return;
        }
    }

    // ���� ��¥�� ���� ���̵� ���� (0 ~ 1) ���
    private float GetDifficultyT()
    {
        return Mathf.Clamp01((currentDay - 1) / 6f);
    }

    // --- �ܺ�(HandInputSystem)���� ������ ��ġ�� ---

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
        if (currentDay > 7) Debug.Log("��� ��¥ Ŭ����!");
    }
}