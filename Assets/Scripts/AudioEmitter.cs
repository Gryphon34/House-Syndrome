using System.Collections;
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

    [Header("재생 조건 (선택)")]
    [Tooltip("지정하면, 이 오브젝트가 활성화되어 있을 때만 재생합니다.\n" +
             "일반맵 오디오 → WalkingPlayer 지정 (악몽맵에서는 재생 안 함)\n" +
             "N일차 맵 오디오 → 해당 N일차 일반맵 루트 지정 (다른 날에는 재생 안 함)")]
    [SerializeField] private GameObject playOnlyWhenActive;

    private AudioSource _audioSource;
    private Coroutine _playbackRoutine;
    private Coroutine _doorDelayRoutine;
    private bool _contextWasActive;

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

        _contextWasActive = IsContextActive();

        // AudioManager 실행 순서 보장: 한 프레임 뒤에 재생 시작 (매번 활성화 시 정상 재생)
        if (_playbackRoutine != null)
        {
            StopCoroutine(_playbackRoutine);
        }

        _playbackRoutine = StartCoroutine(StartPlaybackNextFrame());
    }

    private void Update()
    {
        if (playOnlyWhenActive == null)
        {
            return;
        }

        bool contextActive = playOnlyWhenActive.activeInHierarchy;

        if (contextActive != _contextWasActive)
        {
            _contextWasActive = contextActive;
            if (contextActive)
            {
                if (playOnEnable && _audioSource != null && !_audioSource.isPlaying)
                {
                    StartPlayback();
                }
            }
            else
            {
                if (_audioSource != null && _audioSource.isPlaying)
                {
                    _audioSource.Stop();
                }
            }
        }
    }

    private bool IsContextActive()
    {
        return playOnlyWhenActive == null || playOnlyWhenActive.activeInHierarchy;
    }

    private void OnDisable()
    {
        if (_playbackRoutine != null)
        {
            StopCoroutine(_playbackRoutine);
            _playbackRoutine = null;
        }
        if (_doorDelayRoutine != null)
        {
            StopCoroutine(_doorDelayRoutine);
            _doorDelayRoutine = null;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnMasterVolumeChanged -= HandleMasterVolumeChanged;
        }

        if (_audioSource != null && _audioSource.isPlaying)
        {
            _audioSource.Stop();
        }
    }

    private IEnumerator StartPlaybackNextFrame()
    {
        yield return null;
        _playbackRoutine = null;
        if (isActiveAndEnabled && playOnEnable && IsContextActive())
        {
            StartPlayback();
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
        if (_audioSource == null)
            return;

        float delaySeconds = (AudioManager.Instance != null) ? AudioManager.Instance.GetPlayDelaySeconds(soundKey) : 0f;
        if (delaySeconds > 0f)
        {
            if (_doorDelayRoutine != null)
                StopCoroutine(_doorDelayRoutine);
            _doorDelayRoutine = StartCoroutine(PlayAfterDelay(delaySeconds));
            return;
        }

        PlayImmediate();
    }

    private IEnumerator PlayAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        _doorDelayRoutine = null;
        if (isActiveAndEnabled && IsContextActive())
            PlayImmediate();
    }

    private void PlayImmediate()
    {
        if (_audioSource == null)
            return;

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("AudioManager 인스턴스가 씬에 없습니다.", this);
            return;
        }

        if (!AudioManager.Instance.TryGetClip(soundKey, out var clip) || clip == null)
        {
            Debug.LogWarning($"AudioEmitter: 키 \"{soundKey}\" 에 해당하는 클립을 AudioManager 에서 찾지 못했습니다.", this);
            return;
        }

        _audioSource.clip = clip;
        _audioSource.loop = loop; // 재생 직전에 루프 확실히 적용 (다른 스크립트/기본값에 덮어씌워지는 것 방지)
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
