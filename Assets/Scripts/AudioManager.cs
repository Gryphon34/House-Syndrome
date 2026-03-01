using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씬에 하나만 두고 사용하는 오디오 매니저.
/// - 인스펙터에서 키(String)와 AudioClip을 1:N으로 관리
/// - 다른 오브젝트들은 키(String)만 알고 있으면 됨
/// - 전체 볼륨(Master Volume)을 한 곳에서 조절
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Serializable]
    public class AudioEntry
    {
        [Tooltip("이 오디오를 가리키는 키 입니다. 여러 오브젝트가 같은 키를 사용할 수 있습니다.")]
        public string key;
        public AudioClip clip;
        [Tooltip("이 오디오 에셋의 개별 볼륨입니다. (0~1)")]
        [Range(0f, 1f)]
        public float volume = 1f;
    }

    [Header("Audio Library")]
    [Tooltip("하나의 AudioClip 을 여러 키로 등록할 수도 있습니다.")]
    [SerializeField] private List<AudioEntry> audioEntries = new List<AudioEntry>();

    [Header("Volume")]
    [Tooltip("전체 소리 크기를 조절합니다. (0~1)")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;

    [Header("키별 재생 지연 (AudioEmitter)")]
    [Tooltip("'door' 키: AudioEmitter가 활성화된 후 이 시간(초) 뒤에 재생합니다. 0이면 지연 없음.")]
    [SerializeField] private float doorSoundDelaySeconds = 120f;

    private readonly Dictionary<string, AudioClip> _clipByKey = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, float> _volumeByKey = new Dictionary<string, float>();

    /// <summary>
    /// 마스터 볼륨 값. 0~1 사이로 Clamp 됩니다.
    /// 이 값을 바꾸면 등록된 리스너들에게 이벤트가 전달됩니다.
    /// </summary>
    public float MasterVolume
    {
        get => masterVolume;
        set
        {
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(clamped, masterVolume))
            {
                return;
            }

            masterVolume = clamped;
            OnMasterVolumeChanged?.Invoke(masterVolume);
        }
    }

    /// <summary>
    /// 마스터 볼륨이 바뀔 때 호출되는 이벤트입니다.
    /// </summary>
    public event Action<float> OnMasterVolumeChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("중복된 AudioManager 가 발견되어 파괴됩니다.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildLookup();

        // 인스펙터 값 반영 및 리스너들에게 초기값 전달
        MasterVolume = masterVolume;
    }

    private void OnValidate()
    {
        // 에디터에서 슬라이더를 조절했을 때도 이벤트가 정상적으로 전달되도록 처리
        MasterVolume = masterVolume;
    }

    private void BuildLookup()
    {
        _clipByKey.Clear();
        _volumeByKey.Clear();

        foreach (var entry in audioEntries)
        {
            if (entry == null || entry.clip == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.key))
            {
                Debug.LogWarning($"키가 비어있는 AudioEntry 가 있습니다. Clip: {entry.clip.name}", this);
                continue;
            }

            if (_clipByKey.ContainsKey(entry.key))
            {
                Debug.LogWarning($"중복된 오디오 키가 발견되었습니다: {entry.key}", this);
                continue;
            }

            _clipByKey.Add(entry.key, entry.clip);
            _volumeByKey.Add(entry.key, Mathf.Clamp01(entry.volume));
        }
    }

    public bool TryGetClip(string key, out AudioClip clip)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            clip = null;
            return false;
        }

        return _clipByKey.TryGetValue(key, out clip);
    }

    /// <summary>
    /// 특정 키에 대해 재생 지연 시간(초)을 반환합니다. 지연이 없으면 0을 반환합니다.
    /// (예: 'door' 키는 doorSoundDelaySeconds 값 사용)
    /// </summary>
    public float GetPlayDelaySeconds(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return 0f;
        if (string.Equals(key, "door", StringComparison.OrdinalIgnoreCase) && doorSoundDelaySeconds > 0f)
            return doorSoundDelaySeconds;
        return 0f;
    }

    /// <summary>
    /// 특정 키에 설정된 개별 볼륨(0~1)을 반환합니다. 없으면 1을 반환합니다.
    /// </summary>
    public float GetEntryVolume(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return 1f;
        }

        if (_volumeByKey.TryGetValue(key, out var v))
        {
            return v;
        }

        return 1f;
    }

    /// <summary>
    /// 위치 기반으로 1회 재생하고 싶은 경우에 사용 (예: 폭발음 등)
    /// </summary>
    public void PlayOneShotAt(string key, Vector3 position, float volume = 1f)
    {
        if (!TryGetClip(key, out var clip))
        {
            Debug.LogWarning($"AudioManager: 키 \"{key}\" 에 해당하는 클립을 찾을 수 없습니다.");
            return;
        }

        float entryVolume = GetEntryVolume(key);
        float finalVolume = Mathf.Clamp01(volume) * entryVolume * MasterVolume;
        if (finalVolume <= 0f)
        {
            return;
        }

        AudioSource.PlayClipAtPoint(clip, position, finalVolume);
    }
}
