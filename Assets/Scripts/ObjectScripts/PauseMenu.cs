using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public sealed class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private UnityEngine.UI.Button pauseButton;
    [SerializeField] private UnityEngine.UI.Slider musicSlider;
    [SerializeField] private UnityEngine.UI.Slider sfxSlider;
    [SerializeField] private FMODUnity.StudioEventEmitter levelMusic;
    [SerializeField] private UnityEngine.UI.Toggle muteAllToggle;
    private float previousTimeScale = 1f;
    private bool paused;
    private float musicVolume;
    private float sfxVolume;
    private float nextAudioUpdate;

    private void Awake()
    {
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SfxVolume", 1f);
        musicSlider.SetValueWithoutNotify(musicVolume);
        sfxSlider.SetValueWithoutNotify(sfxVolume);
        pausePanel.SetActive(false);
        bool muted = PlayerPrefs.GetInt("AudioMuted", 0) == 1;
        if (muteAllToggle != null) muteAllToggle.SetIsOnWithoutNotify(muted);
        GlobalAudioMute.ApplySavedState();
    }

    private void Update()
    {
        bool canPause = GameManager.Instance != null && !GameManager.Instance.IsComplete &&
            PlayerController.Instance != null && PlayerController.Instance.enabled;
        pauseButton.interactable = canPause;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (paused) Resume();
            else if (canPause) Pause();
        }
        if (Time.unscaledTime >= nextAudioUpdate)
        {
            nextAudioUpdate = Time.unscaledTime + 0.05f;
            ApplyVolumes();
        }
    }

    public void Pause()
    {
        if (paused || GameManager.Instance == null || GameManager.Instance.IsComplete ||
            PlayerController.Instance == null || !PlayerController.Instance.enabled) return;
        previousTimeScale = Time.timeScale;
        paused = true;
        Time.timeScale = 0f;
        FMODUnity.RuntimeManager.GetBus("bus:/").setPaused(true);
        pausePanel.SetActive(true);
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
    }

    public void Resume()
    {
        if (!paused) return;
        paused = false;
        Time.timeScale = previousTimeScale;
        FMODUnity.RuntimeManager.GetBus("bus:/").setPaused(false);
        pausePanel.SetActive(false);
        bool muted = PlayerPrefs.GetInt("AudioMuted", 0) == 1;
        if (muteAllToggle != null) muteAllToggle.SetIsOnWithoutNotify(muted);
        GlobalAudioMute.ApplySavedState();
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
    }

    public void OpenMenu()
    {
        if (!Application.CanStreamedLevelBeLoaded("LevelScene")) return;
        Resume();
        FMODUnity.RuntimeManager.GetBus("bus:/").stopAllEvents(FMOD.Studio.STOP_MODE.IMMEDIATE);
        SceneTransition.Load("LevelScene");
    }

    public void SetAllAudioMuted(bool muted)
    {
        GlobalAudioMute.SetMuted(muted);
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        ApplyVolumes();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("SfxVolume", sfxVolume);
        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        if (FMODUnity.RuntimeManager.StudioSystem.getBankList(out var banks) != FMOD.RESULT.OK) return;
        FMOD.GUID musicId = levelMusic != null ? levelMusic.EventReference.Guid : default;
        foreach (var bank in banks)
        {
            if (bank.getEventList(out var events) != FMOD.RESULT.OK) continue;
            foreach (var description in events)
            {
                description.getID(out var id);
                float volume = levelMusic != null && id.Equals(musicId) ? musicVolume : sfxVolume;
                if (description.getInstanceList(out var instances) != FMOD.RESULT.OK) continue;
                foreach (var instance in instances) instance.setVolume(volume);
            }
        }
    }

    private void OnDisable()
    {
        if (paused) Resume();
        PlayerPrefs.Save();
    }
}




