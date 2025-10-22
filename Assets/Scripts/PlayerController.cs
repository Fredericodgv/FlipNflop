using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float jumpForce = 12.0f;
    [SerializeField] private float groundCheckRadius = 0.2f;

    [Header("Gameplay Settings")]
    [SerializeField] private float fallKillThreshold = -25f;

    [Header("Object References")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private PathVerifier pathVerifier;
    [SerializeField] private CameraController cameraController;

    public bool IsGravityInverted => rb.gravityScale < 0;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private float horizontalInput;
    private bool isGrounded;
    private bool jumpInput;
    private bool gravityFlipInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (gameOverUI != null) gameOverUI.SetActive(false);
        rb.gravityScale = Mathf.Abs(rb.gravityScale);
    }

    private void Update()
    {
        HandleInput();
        CheckWinAndLoseConditions();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        CheckIfGrounded();
        HandleMovement();
        HandleJump();
        HandleGravityFlip();
    }

    #region Input & State Checks

    private void HandleInput()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        if (Input.GetButtonDown("Jump")) { jumpInput = true; }
        if (Input.GetKeyDown(KeyCode.W)) { gravityFlipInput = true; }
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

    private void HandleMovement()
    {
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
        Flip();
    }

    private void Flip()
    {
        if (Mathf.Abs(horizontalInput) < 0.1f) return;
        bool wantsToGoLeft = horizontalInput < 0;
        spriteRenderer.flipX = wantsToGoLeft ^ IsGravityInverted;
    }

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

    private void PlayerDeath()
    {
        // Usamos a sua lógica original para o Game Over.
        if (gameOverUI != null) gameOverUI.SetActive(true);
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
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
    }
}