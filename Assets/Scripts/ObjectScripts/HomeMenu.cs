using UnityEngine;

public sealed class HomeMenu : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private UnityEngine.UI.Slider musicSlider;
    [SerializeField] private UnityEngine.UI.Slider sfxSlider;
    [SerializeField] private UnityEngine.UI.Toggle muteToggle;
    [Header("FMOD background music")]
    [SerializeField] private FMODUnity.EventReference backgroundMusicEvent;


    private void Awake()
    {
        settingsPanel.SetActive(false);
        musicSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MusicVolume", 1f));
        sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("SfxVolume", 1f));
        bool muted = PlayerPrefs.GetInt("AudioMuted", 0) == 1;
        muteToggle.SetIsOnWithoutNotify(muted);
        GlobalAudioMute.ApplySavedState();
    }

    private void Start() => PlayBackgroundMusic();

    public void PlayBackgroundMusic() => MenuBackgroundMusic.Play(backgroundMusicEvent);
    public void StopBackgroundMusic() => MenuBackgroundMusic.Stop();
    private void OnDestroy() => PlayerPrefs.Save();

    public void Play() => SceneTransition.Load("LevelScene");
    public void OpenSettings() => settingsPanel.SetActive(true);
    public void CloseSettings() { settingsPanel.SetActive(false); PlayerPrefs.Save(); }
    public void SetMusicVolume(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("MusicVolume", value);
        MenuBackgroundMusic.SetVolume(value);
    }
    public void SetSfxVolume(float value) => PlayerPrefs.SetFloat("SfxVolume", Mathf.Clamp01(value));
    public void SetMuted(bool muted)
    {
        GlobalAudioMute.SetMuted(muted);
    }
}


