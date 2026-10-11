using UnityEngine;

[DisallowMultipleComponent]
public sealed class DescendingCeiling : MonoBehaviour
{
    [SerializeField, Min(0f)] private float raisedHeight = 16f;
    [SerializeField, Min(0.01f)] private float descentDuration = 1.5f;

    private Vector3 loweredPosition;
    private Vector3 raisedPosition;
    private Renderer[] renderers;
    private Collider2D[] colliders;
    private bool[] rendererStates;
    private bool[] colliderStates;
    private float elapsed;
    private bool descending;
    private bool triggered;

    private void Awake()
    {
        loweredPosition = transform.localPosition;
        raisedPosition = loweredPosition + Vector3.up * raisedHeight;
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider2D>(true);
        rendererStates = new bool[renderers.Length];
        colliderStates = new bool[colliders.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            rendererStates[i] = renderers[i].enabled;
            renderers[i].enabled = false;
        }
        for (int i = 0; i < colliders.Length; i++)
        {
            colliderStates[i] = colliders[i].enabled;
            colliders[i].enabled = false;
        }
        transform.localPosition = raisedPosition;
    }

    public void BeginDescent()
    {
        if (triggered) return;
        triggered = true;
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = rendererStates[i];
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = colliderStates[i];
        elapsed = 0f;
        descending = true;
    }

    private void FixedUpdate()
    {
        if (!descending) return;
        elapsed += Time.fixedDeltaTime;
        float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, descentDuration));
        transform.localPosition = Vector3.Lerp(raisedPosition, loweredPosition,
            Mathf.SmoothStep(0f, 1f, progress));
        if (progress >= 1f) descending = false;
    }
}
