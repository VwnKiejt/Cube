using UnityEngine;

public static class GlobalAudioMute
{
    private const string PreferenceKey = "AudioMuted";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void ApplySavedState()
    {
        Apply(PlayerPrefs.GetInt(PreferenceKey, 0) == 1);
    }

    public static void SetMuted(bool muted)
    {
        PlayerPrefs.SetInt(PreferenceKey, muted ? 1 : 0);
        Apply(muted);
        PlayerPrefs.Save();
    }

    private static void Apply(bool muted)
    {
        AudioListener.volume = muted ? 0f : 1f;
        // The Core master group also covers sounds played outside Studio buses.
        var result = FMODUnity.RuntimeManager.CoreSystem.getMasterChannelGroup(out var master);
        if (result == FMOD.RESULT.OK) result = master.setMute(muted);
        if (result != FMOD.RESULT.OK)
            Debug.LogWarning($"Could not apply global FMOD mute: {result}");
    }
}
