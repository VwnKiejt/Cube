using UnityEngine;

public class BeatPulse : MonoBehaviour
{
    [SerializeField] private float sensitivity = 1f;
    [SerializeField, Min(1f)] private float maxScaleMultiplier = 1.35f;
    [SerializeField] private float attackSpeed = 24f;
    [SerializeField] private float releaseSpeed = 10f;
    [SerializeField, Range(0f, 1f)] private float musicInfluence = 0.35f;
    [SerializeField, Range(0f, 1f)] private float beatImpact = 0.65f;
    [SerializeField] private float beatReleaseSpeed = 4f;

    private Vector3 baseScale;
    private float smoothedAmount;
    private float beatAmount;
    private AudioManager audioManager;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        BindAudioManager();
    }

    private void Start() => BindAudioManager();

    private void OnDisable()
    {
        if (audioManager != null)
            audioManager.OnBeat -= HandleBeat;
        audioManager = null;
    }

    private void BindAudioManager()
    {
        if (audioManager != null || AudioManager.instance == null) return;
        audioManager = AudioManager.instance;
        audioManager.OnBeat += HandleBeat;
    }

    private void HandleBeat()
    {
        beatAmount = 1f;
    }

    private void Update()
    {
        BindAudioManager();
        float musicAmount = audioManager != null
            ? Mathf.Clamp01(audioManager.StereoBassLevel * sensitivity)
            : 0f;

        beatAmount = Mathf.MoveTowards(beatAmount, 0f, beatReleaseSpeed * Time.deltaTime);
        float musicContribution = musicAmount * musicInfluence;
        float waveAmount = musicContribution +
                           beatAmount * beatImpact * (1f - musicContribution);
        float speed = waveAmount > smoothedAmount ? attackSpeed : releaseSpeed;
        smoothedAmount = Mathf.MoveTowards(
            smoothedAmount,
            waveAmount,
            speed * Time.deltaTime
        );

        float scaleMultiplier = Mathf.Lerp(1f, maxScaleMultiplier, smoothedAmount);
        transform.localScale = new Vector3(
            baseScale.x * scaleMultiplier,
            baseScale.y * scaleMultiplier,
            baseScale.z
        );
    }
}
