using UnityEngine;

public static class LevelProgressSave
{
    private static string Key(string scenePath) => "LevelBestProgress:" + scenePath;
    public static float GetBest(string scenePath) => PlayerPrefs.GetFloat(Key(scenePath), 0f);
    public static void Reset(string scenePath)
    {
        PlayerPrefs.DeleteKey(Key(scenePath));
        PlayerPrefs.Save();
    }

    public static void Record(string scenePath, float progress)
    {
        progress = Mathf.Clamp01(progress);
        if (progress > GetBest(scenePath)) PlayerPrefs.SetFloat(Key(scenePath), progress);
    }
}

