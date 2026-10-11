using UnityEngine;

public sealed class LevelProgressBar : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image fill;
    [SerializeField] private FMODUnity.StudioEventEmitter levelMusic;
    [SerializeField] private AudioClip levelAudio;
    private float duration;
    private float progress;

    private void Awake()
    {
        duration = levelAudio != null ? levelAudio.length : 0f;
        fill.fillAmount = 0f;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsComplete)
        {
            progress = 1f;
            LevelProgressSave.Record(gameObject.scene.path, progress);
            fill.fillAmount = 1f;
            return;
        }
        if (levelMusic != null && levelMusic.EventInstance.isValid())
        {
            var instance = levelMusic.EventInstance;
            if (instance.getDescription(out var description) == FMOD.RESULT.OK &&
                description.getLength(out int length) == FMOD.RESULT.OK && length > 0)
                duration = length / 1000f;
            if (duration > 0f && instance.getTimelinePosition(out int milliseconds) == FMOD.RESULT.OK)
                progress = Mathf.Max(progress, Mathf.Clamp01(milliseconds / (duration * 1000f)));
        }
        else if (AudioManager.instance != null && duration > 0f)
        {
            var source = AudioManager.instance.MusicSource;
            if (source != null && source.isPlaying)
                progress = Mathf.Max(progress, Mathf.Clamp01(source.time / duration));
        }
        LevelProgressSave.Record(gameObject.scene.path, progress);
        fill.fillAmount = progress;
    }
    private void OnDisable()
    {
        LevelProgressSave.Record(gameObject.scene.path, progress);
        PlayerPrefs.Save();
    }
}


