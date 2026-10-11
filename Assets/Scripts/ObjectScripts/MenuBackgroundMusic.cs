using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MenuBackgroundMusic : MonoBehaviour
{
    private static MenuBackgroundMusic instance;
    private FMOD.Studio.EventInstance music;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() => instance = null;
    public static void Play(FMODUnity.EventReference audioEvent)
    {
        if (audioEvent.IsNull) return;
        if (instance == null) instance = new GameObject("MenuBackgroundMusic").AddComponent<MenuBackgroundMusic>();
        if (instance.music.isValid()) return;
        instance.music = FMODUnity.RuntimeManager.CreateInstance(audioEvent);
        instance.music.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(Vector3.zero));
        SetVolume(PlayerPrefs.GetFloat("MusicVolume", 1f));
        GlobalAudioMute.ApplySavedState();
        instance.music.start();
    }
    public static void SetVolume(float value)
    {
        if (instance != null && instance.music.isValid()) instance.music.setVolume(Mathf.Clamp01(value));
    }
    public static void Stop()
    {
        if (instance == null || !instance.music.isValid()) return;
        instance.music.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        instance.music.release();
        instance.music.clearHandle();
    }
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += SceneLoaded;
    }
    private void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "HomeScene" && scene.name != "LevelScene") Stop();
    }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        if (instance == this) { Stop(); instance = null; }
    }
}

