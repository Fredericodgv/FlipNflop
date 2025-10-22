using System.Collections;
using System.Collections.Generic;
using System;
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
    [SerializeField] private float groundCheckRadius = 0.2f;

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

    [Header("Object References")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private PathVerifier pathVerifier;
    [SerializeField] private CameraController cameraController;

    [Header("Input System")]
    [Tooltip("Reference to the Move action (1D Axis/float).")]
    [SerializeField] private InputActionReference moveAction;
    [Tooltip("Reference to the Jump action (Button).")]
    [SerializeField] private InputActionReference jumpAction;
    [Tooltip("Reference to the Flip Gravity action (Button).")]
    [SerializeField] private InputActionReference flipGravityAction;

    /// <summary>
    /// Indicates whether gravity is inverted (gravityScale &lt; 0).
    /// </summary>
    public bool IsGravityInverted => rb.gravityScale < 0;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private float horizontalInput;
    private bool isGrounded;
    private bool jumpInput;
    private bool gravityFlipInput;


    /// <summary>
    /// Initializes references to required components.
    /// </summary>
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Sets the initial state and ensures gravity is positive.
    /// </summary>
    private void Start()
    {
        if (gameOverUI != null) gameOverUI.SetActive(false);
        rb.gravityScale = Mathf.Abs(rb.gravityScale);
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

        if (transform.position.x > LevelManager.Instance.levelEndX)
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
        float targetX = horizontalInput * moveSpeed;
        float currentX = rb.linearVelocity.x;
        bool hasInput = Mathf.Abs(horizontalInput) > 0.01f;
        float accel = hasInput ? acceleration : deceleration;
        if (!isGrounded) accel *= airControlMultiplier;
        float newX = Mathf.MoveTowards(currentX, targetX, accel * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
        Flip();
    }

    /// <summary>
    /// Sets the sprite orientation based on direction and gravity.
    /// </summary>
    private void Flip()
    {
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
            float jumpDirection = Mathf.Sign(rb.gravityScale);
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
            animator.SetTrigger("jump");
        }
        gravityFlipInput = false;
    }

    #endregion

    #region Collision & Death

    /// <summary>
    /// Activates the Game Over UI and disables the player object.
    /// </summary>
    private void PlayerDeath()
    {
        if (gameOverUI != null) gameOverUI.SetActive(true);
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
    /// Draws the ground check gizmo in the scene when selected.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
    }
}