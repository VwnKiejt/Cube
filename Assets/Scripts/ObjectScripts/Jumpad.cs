using UnityEngine;

public class Jumpad : MonoBehaviour
{
    [SerializeField] private float jumpForce = 14f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<PlayerController>(out var player))
            return;

        if (player.State != PlayerState.CUBE)
            return;

        player.Rb.linearVelocityY = jumpForce;
        GameManager.Instance.RecordJump();
    }
}
