using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class LevelEnding : MonoBehaviour
{
    [SerializeField] private FMODUnity.EventReference completionEvent;
    [SerializeField] private GameObject disappearanceEffect;
    [SerializeField, Min(0f)] private float riseHeight = 2.5f;
    [SerializeField, Min(0.01f)] private float riseDuration = 0.35f;
    [SerializeField, Min(0.01f)] private float spinDuration = 0.5f;
    private bool completed;
    [SerializeField] private LevelResultsPanel resultsPanel;
    private FMOD.Studio.EventInstance endInstance;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerController>();
        if (player != null) Finish(player);
    }

    public void Finish(PlayerController player)
    {
        if (completed || player == null || GameManager.Instance == null ||
            GameManager.Instance.IsComplete) return;
        completed = true;
        GameManager.Instance.CompleteLevel();
        PlayCompletionSound();
        StartCoroutine(PlayEnding(player));
    }

    private IEnumerator PlayEnding(PlayerController player)
    {
        Vector3 start = player.transform.position;
        Vector3 destination = start + Vector3.up * riseHeight;
        float elapsed = 0f;
        while (elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / riseDuration));
            player.transform.position = Vector3.Lerp(start, destination, t);
            yield return null;
        }
        player.transform.position = destination;
        Transform visual = player.SpriteRenderer.transform;
        float startAngle = visual.localEulerAngles.z;
        float duration = Mathf.Max(0.01f, spinDuration);
        // Keep the existing one full turn; speed ramps from zero to this maximum.
        float maxSpinSpeed = 720f / duration;
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed = Mathf.Min(elapsed + Time.deltaTime, duration);
            float progress = elapsed / duration;
            // Integral of speed = maxSpinSpeed * progress: maximum at duration.
            float angle = 0.5f * maxSpinSpeed * elapsed * progress;
            visual.localRotation = Quaternion.Euler(0f, 0f, startAngle - angle);
            yield return null;
        }
        if (disappearanceEffect != null)
        {
            var effect = Instantiate(disappearanceEffect, player.SpriteRenderer.bounds.center,
                Quaternion.identity);
            effect.transform.localScale *= 0.8f;
            float lifetime = 0f;
            foreach (var particles in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particles.main;
                main.loop = false;
                lifetime = Mathf.Max(lifetime, main.startDelay.constantMax +
                    main.duration + main.startLifetime.constantMax);
                particles.GetComponent<ParticleSystemRenderer>().sortingOrder = 100;
            }
            foreach (var particles in effect.GetComponentsInChildren<ParticleSystem>())
                particles.Play();
            Destroy(effect, Mathf.Max(3f, lifetime + 0.5f));
        }
        foreach (var renderer in player.GetComponentsInChildren<SpriteRenderer>())
            renderer.enabled = false;
        while (endInstance.isValid())
        {
            var result = endInstance.getPlaybackState(out var state);
            if (result != FMOD.RESULT.OK || state == FMOD.Studio.PLAYBACK_STATE.STOPPED) break;
            yield return null;
        }
        ReleaseEndSound();
        if (resultsPanel != null) resultsPanel.Show(GameManager.Instance);
    }
    public void PlayCompletionSound()
    {
        if (completionEvent.IsNull) return;
        ReleaseEndSound();
        endInstance = FMODUnity.RuntimeManager.CreateInstance(completionEvent);
        endInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(transform.position));
        endInstance.start();
    }

    private void ReleaseEndSound()
    {
        if (!endInstance.isValid()) return;
        endInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        endInstance.release();
        endInstance.clearHandle();
    }

    private void OnDestroy() => ReleaseEndSound();

}

