using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
public sealed class CollectibleCoin : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField, Min(1f)] private float framesPerSecond = 15f;
    [SerializeField] private FMODUnity.EventReference collectEvent;
    private SpriteRenderer visual;
    private bool collected;

    private void Awake() => visual = GetComponent<SpriteRenderer>();

    private void Update()
    {
        if (!collected && frames != null && frames.Length > 0)
            visual.sprite = frames[Mathf.FloorToInt(Time.time * framesPerSecond) % frames.Length];
    }

    public void PlayCollectSound()
    {
        if (collectEvent.IsNull) return;
        FMODUnity.RuntimeManager.PlayOneShot(collectEvent, transform.position);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || other.GetComponentInParent<PlayerController>() == null ||
            GameManager.Instance == null) return;
        if (!GameManager.Instance.CollectCoin(GetInstanceID())) return;
        collected = true;
        PlayCollectSound();
        visual.enabled = false;
        GetComponent<CircleCollider2D>().enabled = false;
        enabled = false;
    }
}

