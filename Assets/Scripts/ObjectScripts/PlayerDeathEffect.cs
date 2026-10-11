using UnityEngine;

public sealed class PlayerDeathEffect : MonoBehaviour
{
    public void PlayForDuration(float duration)
    {
        var systems = GetComponentsInChildren<ParticleSystem>(true);
        float naturalDuration = 0.01f;
        foreach (var system in systems)
        {
            var main = system.main;
            var emission = system.emission;
            float lastEmission = 0f;
            if (emission.rateOverTime.constantMax > 0f || emission.rateOverDistance.constantMax > 0f)
                lastEmission = main.duration;
            for (int i = 0; i < emission.burstCount; i++)
            {
                var burst = emission.GetBurst(i);
                lastEmission = Mathf.Max(lastEmission,
                    burst.time + Mathf.Max(0, burst.cycleCount - 1) * burst.repeatInterval);
            }
            naturalDuration = Mathf.Max(naturalDuration,
                main.startDelay.constantMax + lastEmission + main.startLifetime.constantMax);
        }

        float speed = naturalDuration / Mathf.Max(0.01f, duration);
        foreach (var system in systems)
        {
            system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = false;
            main.stopAction = ParticleSystemStopAction.None;
            main.useUnscaledTime = true;
            main.simulationSpeed = speed;
            system.Play(false);
        }
    }
}
