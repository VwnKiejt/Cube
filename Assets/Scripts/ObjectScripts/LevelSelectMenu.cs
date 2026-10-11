using System.Collections;
using UnityEngine;

public sealed class LevelSelectMenu : MonoBehaviour
{
    [SerializeField] private string levelScenePath = "Assets/Scenes/GameScene.unity";
    [SerializeField] private UnityEngine.UI.Image normalFill;
    [SerializeField] private TMPro.TMP_Text percentageText;
    [SerializeField] private FMODUnity.EventReference backgroundMusicEvent;
    [Header("FMOD play panel sound")]
    [SerializeField] private FMODUnity.EventReference playPanelEvent;
    [SerializeField, Min(0f)] private float soundLeadIn = 0.15f;
    private bool startingLevel;

    private void Start() => MenuBackgroundMusic.Play(backgroundMusicEvent);

    private void OnEnable()
    {
        float progress = LevelProgressSave.GetBest(levelScenePath);
        normalFill.fillAmount = progress;
        percentageText.text = $"{Mathf.FloorToInt(progress * 100f)}%";
    }

    public void PlayPanelSound()
    {
        if (playPanelEvent.IsNull) return;
        var sound = FMODUnity.RuntimeManager.CreateInstance(playPanelEvent);
        sound.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(transform.position));
        sound.setVolume(PlayerPrefs.GetFloat("SfxVolume", 1f));
        sound.start();
        sound.release();
    }

    public void Play()
    {
        if (startingLevel) return;
        startingLevel = true;
        MenuBackgroundMusic.Stop();
        StartCoroutine(StartLevel());
    }

    public void BackToHome()
    {
        if (startingLevel) return;
        startingLevel = true;
        SceneTransition.Load("HomeScene");
    }

    private IEnumerator StartLevel()
    {
        Time.timeScale = 1f;
        PlayPanelSound();
        if (!playPanelEvent.IsNull && soundLeadIn > 0f)
            yield return new WaitForSecondsRealtime(soundLeadIn);
        SceneTransition.Load(levelScenePath);
    }
}


