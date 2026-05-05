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

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip footstepSound;
    [Tooltip("Variação do som dos passos para não parecer uma metralhadora repetitiva.")]
    [SerializeField] private float pitchVariation = 0.1f;


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
    private bool isFlipping;
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

        scoreController = GetOrFind(scoreController, "ScoreController não encontrado!");
        pathVerifier = GetOrFind(pathVerifier, "PathVerifier não encontrado!");
        cameraController = GetOrFind(cameraController, "CameraController não encontrado!");
        hintController = GetOrFind(hintController, "HintController não encontrado!");
    }

    private T GetOrFind<T>(T reference, string errorMessage) where T : Object
    {
        if (reference == null)
            reference = FindFirstObjectByType<T>();

        if (reference == null)
            Debug.LogError(errorMessage);

        return reference;
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
        UpdateAnimator();
        CheckWinAndLoseConditions();
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
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx) => horizontalInput = ctx.ReadValue<float>();
    private void OnMoveCanceled(InputAction.CallbackContext ctx) => horizontalInput = 0f;
    private void OnJumpPerformed(InputAction.CallbackContext ctx) => jumpInput = true;
    private void OnFlipGravityPerformed(InputAction.CallbackContext ctx) => gravityFlipInput = true;
    private void OnDashPerformed(InputAction.CallbackContext ctx) => TryStartDash();

    private void CheckIfGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);
    }

    private void CheckWinAndLoseConditions()
    {
        float limit = Mathf.Abs(fallKillThreshold);
        float y = transform.position.y;

        if (y < -limit || y > limit)
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

    /// <summary>
    /// Applies horizontal movement based on player input.
    /// If the player is currently dashing, overrides movement to maintain constant dash velocity
    /// and locks vertical position.
    /// </summary>
    private void HandleMovement()
    {
        if (isDashing)
        {
            rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);
            rb.position = new Vector2(rb.position.x, dashLockY);
            FlipSprite();
            return;
        }

        float currentX = rb.linearVelocity.x;
        bool hasInput = Mathf.Abs(horizontalInput) > 0.01f;
        float accel = hasInput ? acceleration : deceleration;
        if (!isGrounded) accel *= airControlMultiplier;

        float targetX = horizontalInput * moveSpeed;
        float newX = Mathf.MoveTowards(currentX, targetX, accel * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
        FlipSprite();
    }

    /// <summary>
    /// Updates the sprite orientation based on movement direction and gravity state.
    /// During a dash, orientation follows the dash direction. Otherwise, it reflects
    /// the current horizontal input, adjusted for gravity inversion.
    /// </summary>
    private void FlipSprite()
    {
        if (isFlipping) return;

        if (isDashing)
        {
            spriteRenderer.flipX = (dashDir < 0f) ^ IsGravityInverted;
            return;
        }

        if (Mathf.Abs(horizontalInput) < 0.1f) return;
        bool wantsToGoLeft = horizontalInput < 0;
        spriteRenderer.flipX = wantsToGoLeft ^ IsGravityInverted;
    }

    /// <summary>
    /// Processes jump input when the player is grounded, applying an instantaneous
    /// vertical velocity in the direction opposite to gravity. Triggers the jump animation.
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
    /// Handles gravity inversion when triggered by input. Flips the gravity scale,
    /// rotates the player, updates sprite orientation, and toggles the logical gravity state.
    /// Only allowed when grounded and not dashing.
    /// </summary>
    private void HandleGravityFlip()
    {
        if (gravityFlipInput && isGrounded && !isDashing && !isFlipping)
        {
            isFlipping = true;

            rb.gravityScale *= -1;
            isGravityInvertedState = !isGravityInvertedState;

            animator.SetTrigger("flip");
        }
        gravityFlipInput = false;
    }

    /// <summary>
    /// Executes the actual gravity inversion and rotation. Should be called at the correct frame by an Animation Event in the Flip animation.
    /// </summary>
    public void ExecuteFlipVisuals()
    {
        transform.Rotate(0f, 0f, 180f);
        spriteRenderer.flipX = !spriteRenderer.flipX;

        isFlipping = false;
    }

    /// <summary>
    /// Starts a dash if off cooldown: chooses direction, locks height, and applies horizontal speed.
    /// </summary>
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

    /// <summary>
    /// Updates the dash state over time. Ends the dash when its duration expires,
    /// restores previous physics settings, and smoothly blends horizontal velocity
    /// back toward player-controlled movement using inertia.
    /// </summary>
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

    /// <summary>
    /// Handles the player's death by stopping the timer, enabling manual camera control
    /// with a right boundary, evaluating the path up to the death position, and disabling
    /// the player GameObject.
    /// </summary>
    private void PlayerDeath()
    {
        scoreController.StopTimer();

        cameraController.EnableManualControlWithRightLimit(transform.position.x);

        pathVerifier.FinalizeAndCheckPath(transform.position.x);

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Detects collisions with other objects. If the player collides with an object
    /// tagged as "Enemy", triggers the death routine.
    /// </summary>
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
    /// Updates animation parameters based on the player's current state, including
    /// running, grounded status, falling condition relative to gravity direction,
    /// and normalized horizontal speed with optional input-based responsiveness.
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

    #region Audio

    /// <summary>
    /// Plays a randomized footstep sound when the player is grounded, ensuring
    /// an audio source and clip are available. Applies slight pitch variation
    /// to avoid repetitive sound effects.
    /// </summary>
    public void PlayFootstepSound()
    {
        audioSource.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);

        audioSource.PlayOneShot(footstepSound);
    }

    #endregion

    /// <summary>
    /// Draws a wireframe sphere in the editor to visualize the ground check area
    /// when the GameObject is selected.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
    }
}