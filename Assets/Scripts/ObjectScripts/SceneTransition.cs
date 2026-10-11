using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneTransition : MonoBehaviour
{
    private static SceneTransition instance;
    private CanvasGroup overlay;
    private bool loading;
    [SerializeField, Min(0.05f)] private float fadeDuration = 0.4f;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() => instance = null;
    public static void Load(string scene)
    {
        if (instance == null)
            instance = new GameObject("SceneTransition").AddComponent<SceneTransition>();
        if (!instance.loading) instance.StartCoroutine(instance.ChangeScene(scene));
    }
    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        var root = new GameObject("FadeCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(UnityEngine.UI.GraphicRaycaster), typeof(CanvasGroup));
        root.transform.SetParent(transform, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
        overlay = root.GetComponent<CanvasGroup>();
        overlay.alpha = 0f;
        overlay.blocksRaycasts = false;
        var black = new GameObject("BlackOverlay", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        black.transform.SetParent(root.transform, false);
        var rect = (RectTransform)black.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        black.GetComponent<UnityEngine.UI.Image>().color = Color.black;
    }
    private IEnumerator ChangeScene(string scene)
    {
        loading = true;
        overlay.blocksRaycasts = true;
        yield return Fade(0f, 1f);
        Time.timeScale = 1f;
        if (FMODUnity.RuntimeManager.IsInitialized)
            FMODUnity.RuntimeManager.GetBus("bus:/").setPaused(false);
        yield return SceneManager.LoadSceneAsync(scene);
        yield return null;
        yield return Fade(1f, 0f);
        overlay.blocksRaycasts = false;
        loading = false;
    }
    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            overlay.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }
        overlay.alpha = to;
    }
}
