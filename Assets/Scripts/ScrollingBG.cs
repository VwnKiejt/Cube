using UnityEngine;

[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(SpriteRenderer))]
public class ScrollingBG : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [Tooltip("0: fixed on screen. 1: scrolls at the camera's speed.")]
    [SerializeField, Range(0f, 1f)] private float scrollFactor = 0.5f;

    private SpriteRenderer background;
    private float startCameraX;
    private float backgroundZ;

    private void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        background = GetComponent<SpriteRenderer>();

        if (targetCamera == null || !targetCamera.orthographic || background.sprite == null)
        {
            Debug.LogError("ScrollingBG needs an orthographic camera and a sprite.", this);
            enabled = false;
            return;
        }

        if (background.sprite.border != Vector4.zero)
        {
            Debug.LogError("Set the background sprite's borders to zero for seamless tiling.", this);
            enabled = false;
            return;
        }

        background.drawMode = SpriteDrawMode.Tiled;
        background.tileMode = SpriteTileMode.Continuous;
        startCameraX = targetCamera.transform.position.x;
        backgroundZ = transform.position.z;
        UpdateBackground();
    }

    private void LateUpdate()
    {
        UpdateBackground();
    }

    private void UpdateBackground()
    {
        if (targetCamera == null || background == null || background.sprite == null) return;

        Vector2 spriteSize = background.sprite.rect.size / background.sprite.pixelsPerUnit;
        float viewHeight = targetCamera.orthographicSize * 2f;
        float viewWidth = viewHeight * targetCamera.aspect;

        // Fit one row to the camera height, preserving the image's proportions.
        float worldScale = viewHeight / spriteSize.y;
        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        if (Mathf.Abs(parentScale.x) < 0.0001f || Mathf.Abs(parentScale.y) < 0.0001f) return;
        transform.localScale = new Vector3(
            worldScale / parentScale.x, worldScale / parentScale.y, transform.localScale.z);

        float tileWidth = spriteSize.x * worldScale;
        background.size = new Vector2(
            (Mathf.Ceil(viewWidth / tileWidth) + 2f) * spriteSize.x, spriteSize.y);

        Vector3 cameraPosition = targetCamera.transform.position;
        float offset = Mathf.Repeat((cameraPosition.x - startCameraX) * scrollFactor, tileWidth);
        Vector3 center = new Vector3(cameraPosition.x - offset, cameraPosition.y, backgroundZ);

        // Account for the sprite pivot instead of requiring a centered source image.
        Vector2 pivot = background.sprite.pivot / background.sprite.rect.size;
        Vector2 pivotOffset = (pivot - Vector2.one * 0.5f) * background.size * worldScale;
        transform.position = center + new Vector3(pivotOffset.x, pivotOffset.y, 0f);
    }
}
