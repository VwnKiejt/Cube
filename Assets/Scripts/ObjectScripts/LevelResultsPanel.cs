using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LevelResultsPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMPro.TMP_Text statistics;
    [SerializeField] private GameObject[] collectedCoins = new GameObject[3];
    [SerializeField] private GameObject[] emptyCoins = new GameObject[3];
    [SerializeField, Min(0f)] private float starRevealInterval = 0.3f;
    [Header("FMOD Menu button sound")]
    [SerializeField] private FMODUnity.EventReference menuButtonEvent;
    private Coroutine starReveal;
    private bool returningToMenu;

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void Show(GameManager manager)
    {
        if (manager == null || panelRoot == null) return;
        StopStarReveal();
        bool revealAllStars = manager.Coins == 3;
        int seconds = Mathf.FloorToInt(manager.ElapsedTime);
        if (statistics != null)
            statistics.text = $"ATTEMPTS: {manager.Attempt}\nJUMPS: {manager.Jumps}\nTIME: {seconds / 60:00}:{seconds % 60:00}";
        for (int i = 0; i < collectedCoins.Length; i++)
            if (collectedCoins[i] != null) collectedCoins[i].SetActive(!revealAllStars && i < manager.Coins);
        for (int i = 0; i < emptyCoins.Length; i++)
            if (emptyCoins[i] != null) emptyCoins[i].SetActive(revealAllStars || i >= manager.Coins);
        panelRoot.SetActive(true);
        if (revealAllStars) starReveal = StartCoroutine(RevealStars());
    }

    private IEnumerator RevealStars()
    {
        for (int i = 0; i < Mathf.Min(3, collectedCoins.Length); i++)
        {
            if (i > 0) yield return new WaitForSecondsRealtime(starRevealInterval);
            if (panelRoot == null || !panelRoot.activeInHierarchy) break;
            if (collectedCoins[i] != null) collectedCoins[i].SetActive(true);
            if (i < emptyCoins.Length && emptyCoins[i] != null) emptyCoins[i].SetActive(false);
        }
        starReveal = null;
    }

    private void StopStarReveal()
    {
        if (starReveal == null) return;
        StopCoroutine(starReveal);
        starReveal = null;
    }

    private void OnDisable() => StopStarReveal();

    public void Replay()
    {
        SceneTransition.Load(SceneManager.GetActiveScene().path);
    }

    public void OpenMenu()
    {
        if (returningToMenu) return;
        if (!Application.CanStreamedLevelBeLoaded("LevelScene"))
        {
            Debug.LogWarning("LevelScene is not enabled in build settings.", this);
            return;
        }
        returningToMenu = true;
        if (!menuButtonEvent.IsNull)
        {
            var sound = FMODUnity.RuntimeManager.CreateInstance(menuButtonEvent);
            sound.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(transform.position));
            sound.setVolume(Mathf.Clamp01(PlayerPrefs.GetFloat("SfxVolume", 1f)));
            sound.start();
            // FMOD owns playback, so the sound can finish after this scene is unloaded.
            sound.release();
        }
        SceneTransition.Load("LevelScene");
    }
}



