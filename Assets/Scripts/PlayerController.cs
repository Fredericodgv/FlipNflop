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
    [SerializeField] private float winPositionX = 25f;

    [Header("Object References")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private GameObject levelClearUI;
    [SerializeField] private GameObject gameOverUI;
    
    public bool IsGravityInverted => rb.gravityScale < 0;

    // --- Componentes ---
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;

    // --- Variáveis de Estado ---
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
        gameOverUI.SetActive(false);
        levelClearUI.SetActive(false);

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

    /// <summary>
    /// Lê e armazena os inputs do jogador.
    /// </summary>
    private void HandleInput()
    {
        horizontalInput = Input.GetAxis("Horizontal");

        if (Input.GetButtonDown("Jump"))
        {
            jumpInput = true;
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            gravityFlipInput = true;
        }
    }

    /// <summary>
    /// Verifica se o jogador está no chão.
    /// </summary>
    private void CheckIfGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);
    }

    /// <summary>
    /// Verifica as condições de morte por queda ou vitória por posição.
    /// </summary>
    private void CheckWinAndLoseConditions()
    {
        if (transform.position.y < fallKillThreshold || transform.position.y > -fallKillThreshold)
        {
            PlayerDeath();
        }

        if (transform.position.x > winPositionX)
        {
            levelClearUI.SetActive(true);
            this.enabled = false;
        }
    }

    #endregion

    #region Movement & Actions

    /// <summary>
    /// Aplica o movimento horizontal e gerencia o flip do personagem.
    /// </summary>
    private void HandleMovement()
    {
        rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);
        Flip();
    }

    /// <summary>
    /// Vira o sprite do personagem para a direção correta do movimento.
    /// </summary>
    private void Flip()
    {
        if (Mathf.Abs(horizontalInput) < 0.1f)
        {
            return;
        }

        bool wantsToGoLeft = horizontalInput < 0;
        spriteRenderer.flipX = wantsToGoLeft ^ IsGravityInverted;
    }

    /// <summary>
    /// Executa a ação de pulo.
    /// </summary>
    private void HandleJump()
    {
        if (jumpInput && isGrounded)
        {
            float jumpDirection = Mathf.Sign(rb.gravityScale);
            rb.velocity = new Vector2(rb.velocity.x, jumpForce * jumpDirection);
        }
        jumpInput = false;
    }
    
    /// <summary>
    /// Executa a ação de inverter a gravidade.
    /// </summary>
    private void HandleGravityFlip()
    {
        if (gravityFlipInput && isGrounded)
        {
            rb.gravityScale *= -1;
            transform.Rotate(0f, 0f, 180f);

            // Compensa a inversão visual da rotação para manter a direção.
            spriteRenderer.flipX = !spriteRenderer.flipX;
        }
        gravityFlipInput = false;
    }

    #endregion

    #region Collision & Death


    /// <summary>
    /// Desativa o jogador e ativa a tela de Game Over.
    /// </summary>
    private void PlayerDeath()
    {
        gameOverUI.SetActive(true);
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

    /// <summary>
    /// Atualiza os parâmetros do Animator.
    /// </summary>
    private void UpdateAnimator()
    {
        bool isRunning = Mathf.Abs(rb.velocity.x) > 0.1f;
        animator.SetBool("Running", isRunning);
        
        animator.SetBool("Jumping", !isGrounded);
    }

    #endregion

    /// <summary>
    /// Desenha um Gizmo na Scene View para visualizar o raio do GroundCheck.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
    }
}