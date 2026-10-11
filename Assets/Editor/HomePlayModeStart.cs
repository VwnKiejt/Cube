using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class HomePlayModeStart
{
    private const string HomeScenePath = "Assets/Scenes/HomeScene.unity";

    static HomePlayModeStart()
    {
        EditorApplication.delayCall += SetHomeScene;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) SetHomeScene();
        };
    }

    private static void SetHomeScene()
    {
        var home = AssetDatabase.LoadAssetAtPath<SceneAsset>(HomeScenePath);
        if (home != null) EditorSceneManager.playModeStartScene = home;
    }
}
