using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerState _currentstate;
    [SerializeField] private Status _currentstatus;
    [SerializeField] private SpeedUp _currentspeed;
    
    [SerializeField] private float currentSpeed;

    [Header("Check")]
    [SerializeField] bool isGrounded;
    [SerializeField] Vector2 groundCheck;
    [SerializeField] float groundCheckRadius = 1.2f;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float jumpRingForce = 14f;
    private bool canJumpFromRing;
    private bool canReverseGravity;

    private SpriteRenderer sr;
    private PlayerMap _map;
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private float defaultGravityScale;
    private RigidbodyConstraints2D defaultConstraints;
    private bool isDead;

    public void Die()
    {
        if (gameManager != null && gameManager.IsComplete) return;
        if (isDead) return;
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        enabled = false;
        gameManager.RestartLevel();
    }

    public void FinishLevel()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;
        enabled = false;
    }

    private void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        _map = new PlayerMap();
        defaultGravityScale = Mathf.Abs(rb.gravityScale);
        rb.gravityScale = defaultGravityScale * GravityDirection;
        defaultConstraints = rb.constraints;
        
    }

    private void Start()
    {
        gameManager.ChangeSpeed(_currentspeed);
        gameManager.EnterState(_currentstate);
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;
        if (_currentstate != PlayerState.SHIP)
            PlayerMovement(_currentstate);

        TryReverseGravity();

        if (_currentstate != PlayerState.SHIP)
            TryJumpFromRing();
    }

    private void TryReverseGravity()
    {
        if (!canReverseGravity || !_map.Player.Jump.WasPressedThisFrame() ||
            _currentstatus == Status.UP)
            return;

        canReverseGravity = false;
        _currentstatus = Status.UP;
        isGrounded = false;
        rb.gravityScale = -Mathf.Abs(rb.gravityScale);
        rb.linearVelocityY = 0f;
    }

    private void TryJumpFromRing()
    {
        if (!canJumpFromRing || isGrounded ||
            !_map.Player.Jump.WasPressedThisFrame())
            return;

        if (_currentstate != PlayerState.CUBE)
            return;

        canJumpFromRing = false;
        rb.linearVelocityY = jumpRingForce * GravityDirection;
        gameManager.RecordJump();
    }

    private bool CheckGrounded()
    {
        if (_currentstatus == Status.DOWN)
            return Physics2D.OverlapCircle((Vector2)transform.position + groundCheck,
                groundCheckRadius, groundLayer);

        Bounds bounds = boxCollider.bounds;
        Vector2 checkPosition = (Vector2)bounds.center +
            new Vector2(groundCheck.x, bounds.extents.y + 0.05f + Mathf.Abs(groundCheck.y));
        Vector2 checkSize = new Vector2(Mathf.Min(bounds.size.x, groundCheckRadius * 2f), 0.1f);
        return Physics2D.OverlapBox(checkPosition, checkSize, 0f, groundLayer);
    }
    private void FixedUpdate()
    {
        if (_currentstate == PlayerState.SHIP) ShipMovement();
        
    }

    private void PlayerMovement(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.SHIP:
                ShipMovement();
                break;
            case PlayerState.CUBE:
                CubeMovement();
                break;

        }
    }


    public void ChangeState(PlayerState newState)
    {
        gameManager.TurnStateInto(newState);
    }

    private void ShipMovement()
    {
        rb.linearVelocityX = currentSpeed;
        float targetSpeed = _map.Player.Jump.IsPressed()
            ? gameManager.Ship.riseSpeed
            : -gameManager.Ship.fallSpeed;
        rb.linearVelocityY = Mathf.MoveTowards(
            rb.linearVelocityY, targetSpeed, gameManager.Ship.verticalAcceleration * Time.fixedDeltaTime);

        rb.angularVelocity = 0f;
        rb.MoveRotation(Mathf.Clamp(rb.linearVelocityY * gameManager.Ship.rotationSpeed, -90f, 90f));
    }

    private void CubeMovement()
    {
        isGrounded = CheckGrounded();
        if (_map.Player.Jump.IsPressed() && isGrounded)
        {
            if (rb.linearVelocityY * GravityDirection <= 0.01f) gameManager.RecordJump();
            rb.linearVelocityY = gameManager.Cube.jumpForce * GravityDirection;
        }

        rb.linearVelocityX = currentSpeed;

        if (!isGrounded || rb.linearVelocityY * GravityDirection > 0.01f)
        {
            float rotationSpeed = 90f / Mathf.Max(gameManager.Cube.rotateDuration, 0.01f);
            sr.transform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime, Space.Self);
        }
        else
        {
            float angle = Mathf.Round(sr.transform.localEulerAngles.z / 90f) * 90f;
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    => CheckBlockCollision(collision);

    private void OnCollisionStay2D(Collision2D collision)
        => CheckBlockCollision(collision);

    private void CheckBlockCollision(Collision2D collision)
    {
        if (State != PlayerState.CUBE && State != PlayerState.SHIP) return;

        // Chỉ kiểm tra nền/block thuộc groundLayer.
        int layer = collision.gameObject.layer;
        if ((groundLayer.value & (1 << layer)) == 0) return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            var contact = collision.GetContact(i);
            // Ignore trailing edges and corner contacts at the floor/ceiling.
            // Only a wall touching the front, inside the body's height, is lethal.
            if (IsBlockingWallContact(contact.normal, contact.point, boxCollider.bounds,
                Mathf.Sign(currentSpeed)))
            {
                Die();
                return;
            }
        }
    }

    private static bool IsBlockingWallContact(Vector2 normal, Vector2 point,
        Bounds bounds, float direction)
    {
        if (direction == 0f || normal.x * direction > -0.9f) return false;
        if ((point.x - bounds.center.x) * direction < 0f) return false;
        float cornerTolerance = Mathf.Min(0.08f, bounds.size.y * 0.1f);
        return point.y > bounds.min.y + cornerTolerance &&
            point.y < bounds.max.y - cornerTolerance;
    }


    private void OnEnable() => _map.Player.Enable();

    private void OnDisable() => _map.Player.Disable();
    private void OnDestroy()
    {
        _map?.Dispose();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere((Vector2)transform.position + groundCheck, groundCheckRadius);
    }

    public SpriteRenderer SpriteRenderer => sr;
    public Rigidbody2D Rb => rb;
    public float DefaultGravityScale => defaultGravityScale;
    public float GravityDirection => _currentstatus == Status.UP ? -1f : 1f;
    public PlayerState State
    {
        get => _currentstate;
        internal set
        {
            if (_currentstate != value)
            {
                sr.transform.localRotation = Quaternion.identity;
            }

            _currentstate = value;
            rb.constraints = value == PlayerState.CUBE
                ? RigidbodyConstraints2D.FreezeRotation
                : defaultConstraints;

            if (value == PlayerState.CUBE)
            {
                rb.angularVelocity = 0f;
                rb.rotation = 0f;
            }
        }
    }
    public float Speed
    {
        get => currentSpeed;
        set => currentSpeed = value;
    }
}



