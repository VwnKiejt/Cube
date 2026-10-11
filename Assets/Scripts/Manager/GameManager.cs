using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public enum PlayerState { CUBE, SHIP, ROBOT, BALL, SPIDER, WAVE}
public enum PortalTransform { CUBE, ROBOT, SHIP, BALL, SPIDER }
public enum SpeedUp {NORMAL, FAST , FASTER, FASTEST}
public enum Status {UP, DOWN}
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] private PlayerController player;
    [SerializeField] private UnityEngine.UI.Text attemptText;
    [SerializeField] private TMPro.TMP_Text attemptWorldText;
    [SerializeField] private FMODUnity.StudioEventEmitter levelMusic;
    [SerializeField] private FMODUnity.EventReference explodeEvent;
    [SerializeField] private PlayerDeathEffect deathEffectPrefab;
    [SerializeField, Min(0.01f)] private float fallbackDeathDuration = 0.6f;
    [SerializeField] private bool enableRespawnBlink = true;
    [SerializeField, Min(0.02f)] private float respawnBlinkInterval = 0.12f;
    private static readonly Dictionary<string, int> attempts = new();
    private static readonly Dictionary<string, int> sessionJumps = new();
    private static readonly Dictionary<string, float> sessionTime = new();
    private readonly HashSet<int> collectedCoins = new();
    public int Coins => Mathf.Min(3, collectedCoins.Count);
    public int Jumps => sessionJumps.TryGetValue(gameObject.scene.path, out int count) ? count : 0;
    public float ElapsedTime => sessionTime.TryGetValue(gameObject.scene.path, out float seconds) ? seconds : 0f;

    public void RecordJump()
    {
        if (!IsComplete && !restarting) sessionJumps[gameObject.scene.path] = Jumps + 1;
    }

    public bool CollectCoin(int id)
    {
        return !IsComplete && !restarting && collectedCoins.Add(id);
    }
    private bool restarting;
    private bool waitingForMusic;
    private float nextBlinkTime;
    public int Attempt { get; private set; }
    public bool IsComplete { get; private set; }
    public FMODUnity.StudioEventEmitter LevelMusic => levelMusic;

    public void CompleteLevel()
    {
        if (IsComplete || restarting) return;
        IsComplete = true;
        waitingForMusic = false;
        player.FinishLevel();
        StopLevelMusic();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        attempts.Clear();
        sessionJumps.Clear();
        sessionTime.Clear();
        Instance = null;
    }

    [Header("Data")]
    [SerializeField] CubeData cube;
    [SerializeField] ShipData ship;

    

    private void Awake()
    {
        Instance = this;
        string scenePath = gameObject.scene.path;
        attempts.TryGetValue(scenePath, out int previous);
        Attempt = previous + 1;
        attempts[scenePath] = Attempt;
        if (attemptText != null) attemptText.text = $"Attempt {Attempt}";
        if (attemptWorldText != null) attemptWorldText.text = $"Attempt {Attempt}";
    }

    public void RestartLevel()
    {
        if (restarting || IsComplete) return;
        restarting = true;
        StopLevelMusic();
        StartCoroutine(ReloadLevel());
    }

    private void Start()
    {
        waitingForMusic = enableRespawnBlink && Attempt > 1;
        nextBlinkTime = Time.unscaledTime + respawnBlinkInterval;
    }

    private void Update()
    {
        if (!IsComplete) sessionTime[gameObject.scene.path] = ElapsedTime + Time.deltaTime;
        if (!waitingForMusic || restarting || player == null) return;

        bool musicPlaying = levelMusic != null && levelMusic.IsPlaying();
        if (AudioManager.instance != null && AudioManager.instance.MusicSource != null)
            musicPlaying |= AudioManager.instance.MusicSource.isPlaying;

        if (musicPlaying)
        {
            waitingForMusic = false;
            player.SpriteRenderer.enabled = true;
            return;
        }

        if (Time.unscaledTime >= nextBlinkTime)
        {
            player.SpriteRenderer.enabled = !player.SpriteRenderer.enabled;
            nextBlinkTime = Time.unscaledTime + respawnBlinkInterval;
        }
    }

    private void StopLevelMusic()
    {
        if (AudioManager.instance != null) AudioManager.instance.StopMusic();
        if (levelMusic == null || levelMusic.EventReference.IsNull) return;

        // Stop any leftover copies of this music event from earlier attempts.
        var description = FMODUnity.RuntimeManager.GetEventDescription(levelMusic.EventReference);
        if (description.getInstanceList(out var instances) == FMOD.RESULT.OK)
        {
            foreach (var music in instances)
                music.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        }
        levelMusic.AllowFadeout = false;
        levelMusic.Stop();
    }

    private IEnumerator ReloadLevel()
    {
        yield return null;

        PlayerDeathEffect effect = null;
        float soundDuration = fallbackDeathDuration;
        if (!explodeEvent.IsNull)
        {
            var description = FMODUnity.RuntimeManager.GetEventDescription(explodeEvent);
            if (description.getLength(out int milliseconds) == FMOD.RESULT.OK && milliseconds > 0)
                soundDuration = milliseconds / 1000f;
        }
        if (deathEffectPrefab != null)
        {
            effect = Instantiate(deathEffectPrefab, player.transform.position, Quaternion.identity);
            effect.PlayForDuration(soundDuration);
        }
        foreach (var renderer in player.GetComponentsInChildren<SpriteRenderer>())
            renderer.enabled = false;

        if (!explodeEvent.IsNull)
        {
            var explode = FMODUnity.RuntimeManager.CreateInstance(explodeEvent);

            try
            {
                explode.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(player.transform.position));
                explode.start();

                yield return null;

                while (explode.isValid())
                {
                    var result = explode.getPlaybackState(out var state);

                    if (result != FMOD.RESULT.OK ||
                        state == FMOD.Studio.PLAYBACK_STATE.STOPPED)
                        break;

                    yield return null;
                }
            }
            finally
            {
                if (explode.isValid())
                    explode.release();
            }
        }
        else
        {
            yield return new WaitForSecondsRealtime(soundDuration);
        }

        if (effect != null) Destroy(effect.gameObject);

        SceneManager.LoadSceneAsync(
            gameObject.scene.path,
            LoadSceneMode.Single
        );
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void GoThroughPortal(PortalTransform portal)
    {

        switch (portal)
        {
            case PortalTransform.CUBE:
                TurnStateInto(PlayerState.CUBE);
                break;
            case PortalTransform.SHIP:
                TurnStateInto(PlayerState.SHIP);
                break;
        }
    }


    public void TurnStateInto(PlayerState newState)
    {
        if (player.State == newState) return;

        ExitState(player.State);
        EnterState(newState);
    }

    public void EnterState(PlayerState state)
    {
        player.State = state;
        SpriteRenderer sr = player.SpriteRenderer;
        Rigidbody2D rb = player.Rb;


        switch (state)
        {
            case PlayerState.CUBE:
                sr.sprite = cube.sprite;
                break;

            case PlayerState.SHIP:
                sr.sprite = ship.sprite;
                rb.gravityScale = 0.5f * player.GravityDirection;
                rb.linearVelocityY = Mathf.Clamp(rb.linearVelocityY * player.GravityDirection,
                    -ship.fallSpeed, ship.riseSpeed) * player.GravityDirection;
                rb.rotation = 0f;
                rb.angularVelocity = 0f;
                break;

        }
    }

    public void ExitState(PlayerState state)
    {
        if (state == PlayerState.SHIP)
        {
            player.Rb.gravityScale = player.DefaultGravityScale * player.GravityDirection;
        }
    }


    public void ChangeSpeed(SpeedUp type)
    {
        switch (type)
        {
            case SpeedUp.NORMAL:
                player.Speed = 10.6f;
                break;
            case SpeedUp.FAST:
                player.Speed = 12.4f;
                break;
        }

    }

    public CubeData Cube => cube;
    public ShipData Ship => ship;

}
