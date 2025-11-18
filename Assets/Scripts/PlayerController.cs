using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls the player's movement, jumping, gravity inversion, and animations.
/// Uses the new Input System and integrates with ground detection, camera, and path verification.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float jumpForce = 12.0f;

    [Header("Ground Check Settings")]
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private LayerMask groundLayer;

    [Header("Animation Settings")]
    [Tooltip("Multiplier applied to the normalized speed to feed the runSpeed parameter.")]
    [SerializeField] private float runAnimSpeedMultiplier = 1f;
    [Tooltip("Damping time for smoothing the runSpeed parameter updates.")]
    [SerializeField] private float runAnimDampTime = 0.05f;
    [Tooltip("Minimum speed multiplier when in run state to avoid animation sticking at start.")]
    [Range(0f, 2f)]
    [SerializeField] private float minRunAnimSpeed = 0.3f;
    [Tooltip("If true, base run animation speed on input target speed (immediate); otherwise use rigidbody velocity (lagged).")]
    [SerializeField] private bool useInputForRunSpeed = true;
    [Header("Movement Smoothing")]
    [Tooltip("How fast the player accelerates toward target speed (units/s^2)")]
    [SerializeField] private float acceleration = 25f;
    [Tooltip("How fast the player slows down when releasing input (units/s^2)")]
    [SerializeField] private float deceleration = 35f;
    [Tooltip("Multiplier applied to acceleration/deceleration while airborne")]
    [SerializeField] private float airControlMultiplier = 0.7f;

    [Header("Gameplay Settings")]
    [SerializeField] private float fallKillThreshold = -25f;

    [Header("Dash Settings")]
    [Tooltip("Horizontal dash speed applied during a dash.")]
    [SerializeField] private float dashSpeed = 18f;
    [Tooltip("Dash duration in seconds.")]
    [SerializeField] private float dashDuration = 0.15f;
    [Tooltip("Cooldown between dashes in seconds.")]
    [SerializeField] private float dashCooldown = 0.5f;
    [Range(0f, 1f)]
    [Tooltip("Inertia factor kept right after dash ends (0 = snap to input speed, 1 = keep full dash speed)")]
    [SerializeField] private float postDashInertiaFactor = 0.5f;

    [Header("Object References")]
    [SerializeField] private PathVerifier pathVerifier;
    [SerializeField] private CameraController cameraController;

    [Header("Input System")]
    [Tooltip("Reference to the Move action (1D Axis/float).")]
    [SerializeField] private InputActionReference moveAction;
    [Tooltip("Reference to the Jump action (Button).")]
    [SerializeField] private InputActionReference jumpAction;
    [Tooltip("Reference to the Flip Gravity action (Button).")]
    [SerializeField] private InputActionReference flipGravityAction;
    [Tooltip("Reference to the Dash action (Button).")]
    [SerializeField] private InputActionReference dashAction;

    /// <summary>
    /// Logical gravity inversion state (independent from temporary physics tweaks like gravityScale = 0 during dash).
    /// </summary>
    public bool IsGravityInverted => isGravityInvertedState;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool isGravityInvertedState;
    private float horizontalInput;
    private bool isGrounded;
    private bool jumpInput;
    private bool gravityFlipInput;
    private bool isDashing;
    private float dashEndTime;
    private float nextDashTime;
    private float dashDir = 1f;
    // Dash physics preservation (Hollow Knight-style height lock)
    private float preDashGravityScale = 1f;
    private RigidbodyConstraints2D preDashConstraints;
    private float dashLockY;

    /// <summary>
    /// Initializes references to required components.
    /// </summary>
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Sets the initial state and ensures gravity is positive.
    /// </summary>
    private void Start()
    {
        rb.gravityScale = Mathf.Abs(rb.gravityScale);
        isGravityInvertedState = rb.gravityScale < 0f ? true : false;
    }

    /// <summary>
    /// Updates win/lose checks and animation parameters per frame.
    /// </summary>
    private void Update()
    {
        CheckWinAndLoseConditions();
        UpdateAnimator();
    }

    /// <summary>
    /// Updates physics: ground check, movement, jump, and gravity flip (fixed steps).
    /// </summary>
    private void FixedUpdate()
    {
        CheckIfGrounded();
        HandleMovement();
        HandleJump();
        HandleGravityFlip();
        HandleDash();
    }


    #region Input & State Checks

    /// <summary>
    /// Subscribes to and enables Input System actions.
    /// </summary>
    private void OnEnable()
    {
        if (moveAction != null && moveAction.action != null)
        {
            moveAction.action.performed += OnMovePerformed;
            moveAction.action.canceled += OnMoveCanceled;
            moveAction.action.Enable();
        }

        if (jumpAction != null && jumpAction.action != null)
        {
            jumpAction.action.performed += OnJumpPerformed;
            jumpAction.action.Enable();
        }

        if (flipGravityAction != null && flipGravityAction.action != null)
        {
            flipGravityAction.action.performed += OnFlipGravityPerformed;
            flipGravityAction.action.Enable();
        }

        if (dashAction != null && dashAction.action != null)
        {
            dashAction.action.performed += OnDashPerformed;
            dashAction.action.Enable();
        }
    }

    /// <summary>
    /// Unsubscribes from and disables Input System actions.
    /// </summary>
    private void OnDisable()
    {
        if (moveAction != null && moveAction.action != null)
        {
            moveAction.action.performed -= OnMovePerformed;
            moveAction.action.canceled -= OnMoveCanceled;
            moveAction.action.Disable();
        }

        if (jumpAction != null && jumpAction.action != null)
        {
            jumpAction.action.performed -= OnJumpPerformed;
            jumpAction.action.Disable();
        }

        if (flipGravityAction != null && flipGravityAction.action != null)
        {
            flipGravityAction.action.performed -= OnFlipGravityPerformed;
            flipGravityAction.action.Disable();
        }

        if (dashAction != null && dashAction.action != null)
        {
            dashAction.action.performed -= OnDashPerformed;
            dashAction.action.Disable();
        }
    }

    /// <summary>
    /// Receives the horizontal movement axis value.
    /// </summary>
    /// <param name="ctx">Input context (axis float).</param>
    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        horizontalInput = ctx.ReadValue<float>();
    }

    /// <summary>
    /// Resets horizontal movement when input is canceled.
    /// </summary>
    /// <param name="ctx">Input context.</param>
    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        horizontalInput = 0f;
    }

    /// <summary>
    /// Flags the jump request (consumed in FixedUpdate).
    /// </summary>
    /// <param name="ctx">Input context.</param>
    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        jumpInput = true;
    }

    /// <summary>
    /// Flags the gravity flip request (consumed in FixedUpdate).
    /// </summary>
    /// <param name="ctx">Input context.</param>
    private void OnFlipGravityPerformed(InputAction.CallbackContext ctx)
    {
        gravityFlipInput = true;
    }

    private void OnDashPerformed(InputAction.CallbackContext ctx)
    {
        TryStartDash();
    }

    /// <summary>
    /// Updates the grounded state via OverlapCircle.
    /// </summary>
    private void CheckIfGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);
    }

    /// <summary>
    /// Checks lose (fall) and level end conditions and triggers PathVerifier/Camera.
    /// </summary>
    private void CheckWinAndLoseConditions()
    {
        if (transform.position.y < fallKillThreshold || transform.position.y > -fallKillThreshold)
        {
            PlayerDeath();
        }

        if (transform.position.x > LevelManager.Instance.levelEndX + 1)
        {
            if (pathVerifier != null)
            {
                pathVerifier.FinalizeAndCheckPath();
            }
            else
            {
                Debug.LogError("Referência para o PathVerifier não definida no PlayerController!");
            }

            if (cameraController != null)
            {
                cameraController.EnableManualControl();
            }
            else
            {
                Debug.LogError("Referência para o CameraController não definida no PlayerController!");
            }

            this.enabled = false;
        }
    }

    #endregion

    #region Movement & Actions

    /// <summary>
    /// Applies acceleration/deceleration to horizontal movement and flips the sprite.
    /// </summary>
    private void HandleMovement()
    {
        // If dashing, override horizontal velocity
        if (isDashing)
        {
            // Maintain constant height and fixed horizontal speed during dash
            rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);
            // Force exact Y lock to avoid micro drift with some physics setups
            rb.position = new Vector2(rb.position.x, dashLockY);
            Flip();
            return;
        }

        float currentX = rb.linearVelocity.x;
        bool hasInput = Mathf.Abs(horizontalInput) > 0.01f;
        float accel = hasInput ? acceleration : deceleration;
        if (!isGrounded) accel *= airControlMultiplier;

        float targetX = horizontalInput * moveSpeed;

        float newX = Mathf.MoveTowards(currentX, targetX, accel * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
        Flip();
    }

    /// <summary>
    /// Sets the sprite orientation based on direction and gravity.
    /// </summary>
    private void Flip()
    {
        // During dash, face dash direction using the same gravity-aware rule used in normal movement
        if (isDashing)
        {
            // Compute facing left for dash without introducing a new local conflicting name
            spriteRenderer.flipX = (dashDir < 0f) ^ IsGravityInverted;
            return;
        }

        if (Mathf.Abs(horizontalInput) < 0.1f) return;
        bool wantsToGoLeft = horizontalInput < 0;
        spriteRenderer.flipX = wantsToGoLeft ^ IsGravityInverted;
    }

    /// <summary>
    /// Performs the jump when requested and grounded.
    /// </summary>
    private void HandleJump()
    {
        if (jumpInput && isGrounded)
        {
            float jumpDirection = IsGravityInverted ? -1f : 1f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * jumpDirection);
            animator.SetTrigger("jump");
        }
        jumpInput = false;
    }

    /// <summary>
    /// Inverts gravity and rotates the character when requested and grounded.
    /// </summary>
    private void HandleGravityFlip()
    {
        if (gravityFlipInput && isGrounded)
        {
            rb.gravityScale *= -1;
            transform.Rotate(0f, 0f, 180f);
            spriteRenderer.flipX = !spriteRenderer.flipX;
            isGravityInvertedState = !isGravityInvertedState;
            animator.SetTrigger("jump");
        }
        gravityFlipInput = false;
    }

    /// <summary>
    /// Starts a dash if off cooldown: chooses direction, locks height, and applies horizontal speed.
    /// </summary>
    private void TryStartDash()
    {
        if (Time.time < nextDashTime) return;

        // Choose dash direction robustly: prefer live input, then current velocity, then current facing
        if (Mathf.Abs(horizontalInput) > 0.01f)
        {
            dashDir = Mathf.Sign(horizontalInput);
        }
        else if (Mathf.Abs(rb.linearVelocity.x) > 0.05f)
        {
            dashDir = Mathf.Sign(rb.linearVelocity.x);
        }
        else
        {
            // Fallback: derive intended left/right from current facing, compensating for gravity inversion
            bool facingLeft = spriteRenderer.flipX ^ IsGravityInverted;
            dashDir = facingLeft ? -1f : 1f;
        }

        // Lock height during dash
        dashLockY = rb.position.y;
        preDashGravityScale = rb.gravityScale;
        preDashConstraints = rb.constraints;
        rb.gravityScale = 0f;
        rb.constraints = preDashConstraints | RigidbodyConstraints2D.FreezePositionY;
        rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);

        isDashing = true;
        dashEndTime = Time.time + dashDuration;
        nextDashTime = Time.time + dashCooldown;
    }

    /// <summary>
    /// Updates/ends the dash and restores physics when its duration elapses.
    /// </summary>
    private void HandleDash()
    {
        if (!isDashing) return;
        if (Time.time >= dashEndTime)
        {
            isDashing = false;
            // Restore physics
            rb.gravityScale = preDashGravityScale;
            rb.constraints = preDashConstraints;

            // Reduce inertia so consecutive dashes don't keep excessive speed
            float currentX = rb.linearVelocity.x;
            float targetX = horizontalInput * moveSpeed;
            float newX = Mathf.Lerp(currentX, targetX, 1f - Mathf.Clamp01(postDashInertiaFactor));
            rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
        }
    }

    #endregion

    #region Collision & Death

    /// <summary>
    /// Activates the Game Over UI and disables the player object.
    /// </summary>
    private void PlayerDeath()
    {
        // Entrega o controle para a câmera com limite direito fixado na posição de morte
        if (cameraController != null)
        {
            cameraController.EnableManualControlWithRightLimit(transform.position.x);
        }
        else
        {
            Debug.LogError("Referência para o CameraController não definida no PlayerController!");
        }

        // Finaliza e avalia o caminho percorrido até o X de morte
        if (pathVerifier != null)
        {
            pathVerifier.FinalizeAndCheckPathUntil(transform.position.x);
        }
        else
        {
            Debug.LogError("Referência para o PathVerifier não definida no PlayerController!");
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Kills the player on collision with enemies.
    /// </summary>
    /// <param name="collision">2D collision data.</param>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            PlayerDeath();
        }
    }

    #endregion

    #region Animation

    /// <summary>
    /// Updates animation parameters (run, fall, run clip speed).
    /// </summary>
    private void UpdateAnimator()
    {
        bool isRunning = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        animator.SetBool("run", isRunning);
        animator.SetBool("grounded", isGrounded);

        float gravitySign = Mathf.Sign(rb.gravityScale);
        bool isFalling = !isGrounded && (rb.linearVelocity.y * gravitySign) < 0f;
        animator.SetBool("fall", isFalling);

        float speedX = useInputForRunSpeed
            ? Mathf.Abs(horizontalInput) * moveSpeed
            : Mathf.Abs(rb.linearVelocity.x);
        float normalized = moveSpeed > 0f ? speedX / moveSpeed : 0f;
        float target = normalized * Mathf.Max(0f, runAnimSpeedMultiplier);
        target = isRunning ? Mathf.Max(target, minRunAnimSpeed) : 0f;
        animator.SetFloat("speed", target, Mathf.Max(0f, runAnimDampTime), Time.deltaTime);
    }

    #endregion

    /// <summary>
    /// Draws the ground check gizmo in the scene when selected
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
    }
}