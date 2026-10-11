using UnityEngine;

/// <summary>
/// Reads original music PCM levels, independently of playback volume and mute.
/// AudioManager supplies the analysis clip and follows the FMOD music timeline.
/// </summary>
public class FmodStereoPulse : MonoBehaviour
{
    [Header("Analysis input")]

    [SerializeField] private bool scaleXFromLeftAndYFromRight = true;
    [SerializeField, Min(0.01f)] private float meterUpdateInterval = 0.033f;

    [Header("Pulse")]
    [SerializeField, Min(0f)] private float stereoThreshold = 0.08f;
    [SerializeField, Min(0f)] private float sensitivity = 5f;
    [SerializeField] private Vector3 maxScale = new Vector3(1.4f, 1.4f, 1f);
    [SerializeField, Min(0f)] private float attackSpeed = 20f;
    [SerializeField, Min(0f)] private float releaseSpeed = 8f;

    private Vector3 baseScale;
    private float smoothedLeft;
    private float smoothedRight;
    private float sampledLeft;
    private float sampledRight;
    private float nextMeterUpdate;



    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextMeterUpdate)
        {
            nextMeterUpdate = Time.unscaledTime + meterUpdateInterval;
            if (!TryReadStereoRms(out sampledLeft, out sampledRight))
            {
                sampledLeft = 0f;
                sampledRight = 0f;
            }
        }

        float leftTarget = ToPulseAmount(sampledLeft);
        float rightTarget = ToPulseAmount(sampledRight);

        if (!scaleXFromLeftAndYFromRight)
            leftTarget = rightTarget = Mathf.Max(leftTarget, rightTarget);

        smoothedLeft = Smooth(smoothedLeft, leftTarget);
        smoothedRight = Smooth(smoothedRight, rightTarget);

        transform.localScale = new Vector3(
            Mathf.Lerp(baseScale.x, maxScale.x, smoothedLeft),
            Mathf.Lerp(baseScale.y, maxScale.y, smoothedRight),
            baseScale.z
        );
    }

    private bool TryReadStereoRms(out float left, out float right)
    {
        var analysis = AudioManager.instance;
        left = analysis != null ? analysis.RmsLeft : 0f;
        right = analysis != null ? analysis.RmsRight : 0f;
        return analysis != null;
    }
    private float ToPulseAmount(float level)
    {
        return Mathf.Clamp01((level - stereoThreshold) * sensitivity);
    }

    private float Smooth(float current, float target)
    {
        float speed = target > current ? attackSpeed : releaseSpeed;
        return Mathf.MoveTowards(current, target, speed * Time.deltaTime);
    }
}
