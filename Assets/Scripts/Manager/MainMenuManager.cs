using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene")]
    public string gameSceneName = "HouseSyndromeScene";

    [Header("Loading UI")]
    public GameObject loadingUIRoot;

    // ──────────────────────────────────────────────
    // BGM
    // ──────────────────────────────────────────────
    [Header("Main Menu BGM")]
    [Tooltip("메인 메뉴에서 재생할 BGM AudioSource (Loop 체크, AudioClip 지정)")]
    public AudioSource bgmSource;

    [Tooltip("씬 전환 시 BGM을 즉시 끄려면 true, 페이드 아웃하려면 false")]
    public bool stopBGMInstantly = false;

    [Tooltip("stopBGMInstantly가 false일 때 페이드 아웃 시간(초)")]
    public float bgmFadeOutDuration = 0.5f;

    [Header("SFX AudioSources (선택)")]
    [Tooltip("버튼 클릭음 등 효과음용 AudioSource 목록. 씬 전환 시 함께 정지됩니다.")]
    public AudioSource[] sfxSources;

    // ──────────────────────────────────────────────
    // 설정 패널
    // ──────────────────────────────────────────────
    [Header("설정 패널")]
    [Tooltip("설정 UI 루트 오브젝트. OpenSettings()로 활성화, 완료/취소로 비활성화됩니다.")]
    public GameObject settingsPanelRoot;

    // ──────────────────────────────────────────────
    // 볼륨 / 감도 슬라이더
    // ──────────────────────────────────────────────
    [Header("볼륨 조절 UI")]
    [Tooltip("전체(마스터) 볼륨 슬라이더 (0~1).")]
    public Slider masterVolumeSlider;

    [Header("마우스 감도 UI")]
    [Tooltip("마우스 감도 슬라이더 (1~100, 퍼센트). 50% = 기본 감도(150).")]
    public Slider sensitivitySlider;

    // PlayerPrefs 저장 키
    private const string KeyMaster = "Vol_Master";

    // 설정 패널을 열었을 때의 값 스냅샷 (취소 시 복원용)
    private float _snapshotMaster;
    private float _snapshotSensitivity;

    // ──────────────────────────────────────────────
    // 초기화
    // ──────────────────────────────────────────────
    private void Start()
    {
        InitSliders();

        // 저장된 값 즉시 적용
        ApplyMasterVolume(PlayerPrefs.GetFloat(KeyMaster, 1f));

        // 설정 패널은 시작 시 닫힌 상태
        if (settingsPanelRoot != null)
            settingsPanelRoot.SetActive(false);

        PlayBGM();
    }

    private void InitSliders()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.value = PlayerPrefs.GetFloat(KeyMaster, 1f);
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
            masterVolumeSlider.onValueChanged.AddListener(OnMasterSliderChanged);
        }

        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = 1f;
            sensitivitySlider.maxValue = 100f;
            sensitivitySlider.wholeNumbers = true;
            sensitivitySlider.value = PlayerPrefs.GetFloat(PlayerLook.KeySensitivity, 50f);
            sensitivitySlider.onValueChanged.RemoveAllListeners();
            sensitivitySlider.onValueChanged.AddListener(OnSensitivitySliderChanged);
        }
    }

    // ──────────────────────────────────────────────
    // 슬라이더 콜백 — 저장 없이 즉시 미리보기만 적용
    // ──────────────────────────────────────────────
    private void OnMasterSliderChanged(float value)
    {
        ApplyMasterVolume(value);
    }

    private void OnSensitivitySliderChanged(float value)
    {
        // 감도는 씬 전환 후 PlayerLook.Awake에서 읽히므로 미리보기 없음
        // 필요 시 여기서 실시간 적용 로직 추가 가능
    }

    // ──────────────────────────────────────────────
    // 설정 패널 열기 / 완료 / 취소
    // ──────────────────────────────────────────────

    /// <summary>설정 패널을 엽니다. 현재 저장된 값을 스냅샷으로 보관합니다.</summary>
    public void OpenSettings()
    {
        // 열기 직전 저장값을 스냅샷으로 보관
        _snapshotMaster      = PlayerPrefs.GetFloat(KeyMaster, 1f);
        _snapshotSensitivity = PlayerPrefs.GetFloat(PlayerLook.KeySensitivity, 50f);

        // 슬라이더를 저장된 값으로 동기화
        if (masterVolumeSlider != null) masterVolumeSlider.value = _snapshotMaster;
        if (sensitivitySlider  != null) sensitivitySlider.value  = _snapshotSensitivity;

        if (settingsPanelRoot != null)
            settingsPanelRoot.SetActive(true);
    }

    /// <summary>현재 슬라이더 값을 PlayerPrefs에 저장하고 패널을 닫습니다.</summary>
    public void ConfirmSettings()
    {
        if (masterVolumeSlider != null)
            PlayerPrefs.SetFloat(KeyMaster, masterVolumeSlider.value);

        if (sensitivitySlider != null)
            PlayerPrefs.SetFloat(PlayerLook.KeySensitivity, sensitivitySlider.value);

        PlayerPrefs.Save();

        if (settingsPanelRoot != null)
            settingsPanelRoot.SetActive(false);
    }

    /// <summary>변경 사항을 버리고 스냅샷 값으로 복원한 뒤 패널을 닫습니다.</summary>
    public void CancelSettings()
    {
        // 슬라이더를 스냅샷 값으로 복원 → OnMasterSliderChanged가 자동 호출되어 오디오도 복원됨
        if (masterVolumeSlider != null) masterVolumeSlider.value = _snapshotMaster;
        if (sensitivitySlider  != null) sensitivitySlider.value  = _snapshotSensitivity;

        if (settingsPanelRoot != null)
            settingsPanelRoot.SetActive(false);
    }

    // ──────────────────────────────────────────────
    // 볼륨 적용
    // ──────────────────────────────────────────────
    private void ApplyMasterVolume(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.MasterVolume = value;

        if (bgmSource != null)
            bgmSource.volume = Mathf.Clamp01(value);

        if (sfxSources != null)
        {
            foreach (var sfx in sfxSources)
                if (sfx != null) sfx.volume = Mathf.Clamp01(value);
        }
    }

    // ──────────────────────────────────────────────
    // BGM 재생
    // ──────────────────────────────────────────────
    private void PlayBGM()
    {
        if (bgmSource == null || bgmSource.isPlaying) return;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    // ──────────────────────────────────────────────
    // 버튼 이벤트
    // ──────────────────────────────────────────────
    public void StartGame()
    {
        StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        yield return StartCoroutine(StopAllAudio());

        if (loadingUIRoot != null)
            loadingUIRoot.SetActive(true);

        AsyncOperation operation = SceneManager.LoadSceneAsync(gameSceneName);
        while (!operation.isDone)
            yield return null;
    }

    private IEnumerator StopAllAudio()
    {
        if (sfxSources != null)
        {
            foreach (var sfx in sfxSources)
                if (sfx != null && sfx.isPlaying) sfx.Stop();
        }

        if (bgmSource == null || !bgmSource.isPlaying)
            yield break;

        if (stopBGMInstantly)
        {
            bgmSource.Stop();
            yield break;
        }

        float startVolume = bgmSource.volume;
        float elapsed = 0f;

        while (elapsed < bgmFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / bgmFadeOutDuration);
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.volume = startVolume;
    }

    public void LoadGame()
    {
        Debug.Log("데이터 로드 기능을 실행합니다.");
    }

    public void ExitGame()
    {
        Debug.Log("게임을 종료합니다.");
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
