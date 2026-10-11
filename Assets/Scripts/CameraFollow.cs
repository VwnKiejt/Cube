using UnityEngine;

[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public sealed class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Range(0.05f, 0.95f)] private float screenX = 0.365f;
    [SerializeField, Min(0.01f)] private float viewSize = 5.5f;
    [SerializeField] private float cameraZ = -10f;
    [SerializeField] private float baseY = 3f;
    [SerializeField] private bool lockY;
    [SerializeField, Range(0f, 1f)] private float lowerY = 0.25f;
    [SerializeField, Range(0f, 1f)] private float upperY = 0.65f;
    [SerializeField, Min(0.01f)] private float smoothY = 0.16f;
    [Header("Level end")]
    [SerializeField] private bool limitRightEdge;
    [SerializeField] private float rightEdgeX;

    private Camera cam;
    private float desiredY;
    private float velocityY;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = Mathf.Max(0.01f, viewSize);
        transform.rotation = Quaternion.identity;
        desiredY = baseY;
    }

    private void Start() => SnapToTarget();

    private float FollowX()
    {
        float width = 2f * cam.orthographicSize * cam.aspect;
        float x = target.position.x + (0.5f - screenX) * width;
        return limitRightEdge ? Mathf.Min(x, rightEdgeX - width * 0.5f) : x;
    }

    private void LateUpdate()
    {
        if (target == null) return;
        cam.orthographicSize = Mathf.Max(0.01f, viewSize);

        if (limitRightEdge && target.position.x + (0.5f - screenX) *
            2f * cam.orthographicSize * cam.aspect >= rightEdgeX - cam.orthographicSize * cam.aspect)
        {
            velocityY = 0f;
            transform.position = new Vector3(FollowX(), transform.position.y, cameraZ);
            return;
        }

        if (lockY)
        {
            desiredY = baseY;
        }
        else
        {
            // Track only when the player leaves the vertical free region.
            float height = 2f * cam.orthographicSize;
            float lowOffset = (lowerY - 0.5f) * height;
            float highOffset = (upperY - 0.5f) * height;
            desiredY = Mathf.Clamp(desiredY,
                target.position.y - highOffset,
                target.position.y - lowOffset);
        }

        float y = Mathf.SmoothDamp(transform.position.y, desiredY,
            ref velocityY, smoothY, Mathf.Infinity, Time.deltaTime);
        transform.position = new Vector3(FollowX(), y, cameraZ);
    }

    // Call on portal/section entry; y is the WORLD-space camera center.
    public void SetSection(float y, bool fixedHeight, bool snap = false)
    {
        baseY = y;
        lockY = fixedHeight;
        desiredY = y;
        velocityY = 0f;
        if (snap)
        {
            Vector3 p = transform.position;
            p.y = y;
            transform.position = p;
        }
    }

    // Call AFTER moving the player to the checkpoint/spawn position.
    public void SnapToTarget()
    {
        if (cam == null) cam = GetComponent<Camera>();
        desiredY = baseY;
        velocityY = 0f;
        if (target != null)
            transform.position = new Vector3(FollowX(), baseY, cameraZ);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        SnapToTarget();
    }

    private void OnValidate()
    {
        upperY = Mathf.Max(upperY, lowerY);
    }
}

