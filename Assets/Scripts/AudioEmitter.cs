using UnityEngine;

/// <summary>
/// 이 스크립트를 가진 오브젝트는 활성화될 때 AudioManager 에 등록된 키의 소리를 재생합니다.
/// - 여러 오브젝트가 같은 키를 사용할 수 있습니다.
/// - 오브젝트가 비활성화되면 소리가 멈춥니다.
/// - AudioManager 의 마스터 볼륨을 따라갑니다.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioEmitter : MonoBehaviour
{
    [Header("Audio")]
    [Tooltip("AudioManager 에서 설정한 키 값입니다.")]
    [SerializeField] private string soundKey;

    [Tooltip("활성화될 때 자동으로 재생할지 여부입니다.")]
    [SerializeField] private bool playOnEnable = true;

    [Tooltip("루프 여부입니다. true 면 오브젝트가 활성화되어있는 동안 계속 재생됩니다.")]
    [SerializeField] private bool loop = true;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = loop;
        ApplyVolume();
    }

    private void OnEnable()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnMasterVolumeChanged += HandleMasterVolumeChanged;
        }

        if (!playOnEnable)
        {
            return;
        }

        StartPlayback();
    }

    private void OnDisable()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnMasterVolumeChanged -= HandleMasterVolumeChanged;
        }

        if (_audioSource != null && _audioSource.isPlaying)
        {
            _audioSource.Stop();
        }
    }

    private void OnValidate()
    {
        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
        }

        if (_audioSource != null)
        {
            _audioSource.loop = loop;
            ApplyVolume();
        }
    }

    private void HandleMasterVolumeChanged(float newMasterVolume)
    {
        ApplyVolume();
    }

    private void ApplyVolume()
    {
        if (_audioSource == null)
        {
            return;
        }

        float master = 1f;
        float entryVolume = 1f;
        if (AudioManager.Instance != null)
        {
            master = AudioManager.Instance.MasterVolume;
            entryVolume = AudioManager.Instance.GetEntryVolume(soundKey);
        }

        _audioSource.volume = Mathf.Clamp01(volume) * entryVolume * master;
    }

    public void StartPlayback()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("AudioManager 인스턴스가 씬에 없습니다.", this);
            return;
        }

        if (!AudioManager.Instance.TryGetClip(soundKey, out var clip))
        {
            Debug.LogWarning($"AudioEmitter: 키 \"{soundKey}\" 에 해당하는 클립을 AudioManager 에서 찾지 못했습니다.", this);
            return;
        }

        _audioSource.clip = clip;
        _audioSource.loop = loop;
        ApplyVolume();
        _audioSource.Play();
    }

    /// <summary>
    /// 키를 런타임에 바꾸고 싶을 때 사용합니다.
    /// (예: 같은 오브젝트에서 상황에 따라 다른 소리 재생)
    /// </summary>
    public void SetSoundKey(string newKey, bool restart = true)
    {
        soundKey = newKey;
        if (restart && isActiveAndEnabled)
        {
            StartPlayback();
        }
    }
}
