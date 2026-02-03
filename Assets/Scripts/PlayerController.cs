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
    [SerializeField] private HintController hintController;
    [SerializeField] private ScoreController scoreController;

    [Header("Input System")]
    [Tooltip("Reference to the Move action (1D Axis/float).")]
    [SerializeField] private InputActionReference moveAction;
    [Tooltip("Reference to the Jump action (Button).")]
    [SerializeField] private InputActionReference jumpAction;
    [Tooltip("Reference to the Flip Gravity action (Button).")]
    [SerializeField] private InputActionReference flipGravityAction;
    [Tooltip("Reference to the Dash action (Button).")]
    [SerializeField] private InputActionReference dashAction;
    [Tooltip("Reference to the Toggle Hints action (Button).")]
    [SerializeField] private InputActionReference toggleHintsAction;

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
    
    // Dash physics preservation
    private float preDashGravityScale = 1f;
    private RigidbodyConstraints2D preDashConstraints;
    private float dashLockY;

    /// <summary>
    /// Initializes references and validates dependencies.
    /// </summary>
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (scoreController == null)
        {
            scoreController = FindFirstObjectByType<ScoreController>();
        }
        if (scoreController == null)
        {
            Debug.LogError("⚠️ ERRO CRÍTICO: 'ScoreController' não encontrado! O Timer não funcionará.");
        }

        if (pathVerifier == null)
        {
            pathVerifier = FindFirstObjectByType<PathVerifier>();
        }
        if (pathVerifier == null)
        {
            Debug.LogError("⚠️ ERRO CRÍTICO: 'PathVerifier' não encontrado! A validação de caminho falhará.");
        }

        if (cameraController == null)
        {
            cameraController = FindFirstObjectByType<CameraController>();
        }
        if (cameraController == null)
        {
            Debug.LogError("⚠️ ERRO CRÍTICO: 'CameraController' não encontrado! A câmera não seguirá o jogador corretamente ao morrer/vencer.");
        }

        if (hintController == null)
        {
            hintController = FindFirstObjectByType<HintController>();
        }
    }

    /// <summary>
    /// Sets the initial state and ensures gravity is positive.
    /// </summary>
    private void Start()
    {
        rb.gravityScale = Mathf.Abs(rb.gravityScale);
        isGravityInvertedState = rb.gravityScale < 0f;
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

    private void OnEnable()
    {
        if (moveAction?.action != null)
        {
            moveAction.action.performed += OnMovePerformed;
            moveAction.action.canceled += OnMoveCanceled;
            moveAction.action.Enable();
        }

        if (jumpAction?.action != null)
        {
            jumpAction.action.performed += OnJumpPerformed;
            jumpAction.action.Enable();
        }

        if (flipGravityAction?.action != null)
        {
            flipGravityAction.action.performed += OnFlipGravityPerformed;
            flipGravityAction.action.Enable();
        }

        if (dashAction?.action != null)
        {
            dashAction.action.performed += OnDashPerformed;
            dashAction.action.Enable();
        }

        if (toggleHintsAction?.action != null)
        {
            toggleHintsAction.action.performed += OnToggleHintsPerformed;
            toggleHintsAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (moveAction?.action != null)
        {
            moveAction.action.performed -= OnMovePerformed;
            moveAction.action.canceled -= OnMoveCanceled;
            moveAction.action.Disable();
        }

        if (jumpAction?.action != null)
        {
            jumpAction.action.performed -= OnJumpPerformed;
            jumpAction.action.Disable();
        }

        if (flipGravityAction?.action != null)
        {
            flipGravityAction.action.performed -= OnFlipGravityPerformed;
            flipGravityAction.action.Disable();
        }

        if (dashAction?.action != null)
        {
            dashAction.action.performed -= OnDashPerformed;
            dashAction.action.Disable();
        }

        if (toggleHintsAction?.action != null)
        {
            toggleHintsAction.action.performed -= OnToggleHintsPerformed;
            toggleHintsAction.action.Disable();
        }
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx) => horizontalInput = ctx.ReadValue<float>();
    private void OnMoveCanceled(InputAction.CallbackContext ctx) => horizontalInput = 0f;
    private void OnJumpPerformed(InputAction.CallbackContext ctx) => jumpInput = true;
    private void OnFlipGravityPerformed(InputAction.CallbackContext ctx) => gravityFlipInput = true;
    private void OnDashPerformed(InputAction.CallbackContext ctx) => TryStartDash();

    private void OnToggleHintsPerformed(InputAction.CallbackContext ctx)
    {
        if (hintController != null)
        {
            hintController.ToggleHintMode();
        }
    }

    private void CheckIfGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);
    }

    private void CheckWinAndLoseConditions()
    {
        if (transform.position.y < fallKillThreshold || transform.position.y > -fallKillThreshold)
        {
            PlayerDeath();
        }

        if (transform.position.x > LevelManager.Instance.levelEndX + 1)
        {
            scoreController.StopTimer();

            rb.linearVelocity = Vector2.zero;
            horizontalInput = 0f;

            pathVerifier.FinalizeAndCheckPath();

            cameraController.EnableManualControl();

            this.enabled = false;
        }
    }

    #endregion

    #region Movement & Actions

    private void HandleMovement()
    {
        if (isDashing)
        {
            rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);
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

    private void Flip()
    {
        if (isDashing)
        {
            spriteRenderer.flipX = (dashDir < 0f) ^ IsGravityInverted;
            return;
        }

        if (Mathf.Abs(horizontalInput) < 0.1f) return;
        bool wantsToGoLeft = horizontalInput < 0;
        spriteRenderer.flipX = wantsToGoLeft ^ IsGravityInverted;
    }

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

    private void HandleGravityFlip()
    {
        if (gravityFlipInput && isGrounded && !isDashing)
        {
            rb.gravityScale *= -1;
            transform.Rotate(0f, 0f, 180f);
            spriteRenderer.flipX = !spriteRenderer.flipX;
            isGravityInvertedState = !isGravityInvertedState;
            animator.SetTrigger("jump");
        }
        gravityFlipInput = false;
    }

    private void TryStartDash()
    {
        if (Time.time < nextDashTime) return;

        if (Mathf.Abs(horizontalInput) > 0.01f)
            dashDir = Mathf.Sign(horizontalInput);
        else if (Mathf.Abs(rb.linearVelocity.x) > 0.05f)
            dashDir = Mathf.Sign(rb.linearVelocity.x);
        else
        {
            bool facingLeft = spriteRenderer.flipX ^ IsGravityInverted;
            dashDir = facingLeft ? -1f : 1f;
        }

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

    private void HandleDash()
    {
        if (!isDashing) return;
        if (Time.time >= dashEndTime)
        {
            isDashing = false;
            rb.gravityScale = preDashGravityScale;
            rb.constraints = preDashConstraints;

            float currentX = rb.linearVelocity.x;
            float targetX = horizontalInput * moveSpeed;
            float newX = Mathf.Lerp(currentX, targetX, 1f - Mathf.Clamp01(postDashInertiaFactor));
            rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
        }
    }

    #endregion

    #region Collision & Death

    private void PlayerDeath()
    {
        // StopTimer sem verificação nula, pois Awake garante que existe
        scoreController.StopTimer();

        // Camera control sem verificação nula
        cameraController.EnableManualControlWithRightLimit(transform.position.x);

        // Verificação de caminho sem verificação nula
        pathVerifier.FinalizeAndCheckPathUntil(transform.position.x);

        gameObject.SetActive(false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            PlayerDeath();
        }
    }

    #endregion

    #region Animation

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

    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
    }
}