using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controla o movimento, salto, inversão de gravidade e animações do jogador.
/// Utiliza o novo Input System e integra com detecção de chão, câmera e verificação de caminho.
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
    /// Indica se a gravidade está invertida (gravityScale &lt; 0).
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
    /// Inicializa as referências aos componentes necessários.
    /// </summary>
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Configura o estado inicial do jogador e garante gravidade positiva.
    /// </summary>
    private void Start()
    {
        if (gameOverUI != null) gameOverUI.SetActive(false);
        rb.gravityScale = Mathf.Abs(rb.gravityScale);
    }

    /// <summary>
    /// Atualiza verificações de vitória/derrota e parâmetros de animação por frame.
    /// </summary>
    private void Update()
    {
        CheckWinAndLoseConditions();
        UpdateAnimator();
    }

    /// <summary>
    /// Atualiza física: chão, movimento, salto e flip de gravidade (em passos fixos).
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
    /// Assina e habilita as ações do Input System.
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
    /// Cancela a assinatura e desabilita as ações do Input System.
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
    /// Recebe o valor do eixo horizontal do movimento.
    /// </summary>
    /// <param name="ctx">Contexto do Input (float do eixo).</param>
    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        horizontalInput = ctx.ReadValue<float>();
    }

    /// <summary>
    /// Reseta o movimento horizontal quando o input é cancelado.
    /// </summary>
    /// <param name="ctx">Contexto do Input.</param>
    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        horizontalInput = 0f;
    }

    /// <summary>
    /// Sinaliza o pedido de salto (consumido no FixedUpdate).
    /// </summary>
    /// <param name="ctx">Contexto do Input.</param>
    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        jumpInput = true;
    }

    /// <summary>
    /// Sinaliza o pedido de inversão de gravidade (consumido no FixedUpdate).
    /// </summary>
    /// <param name="ctx">Contexto do Input.</param>
    private void OnFlipGravityPerformed(InputAction.CallbackContext ctx)
    {
        gravityFlipInput = true;
    }

    /// <summary>
    /// Atualiza o estado de contato com o chão via OverlapCircle.
    /// </summary>
    private void CheckIfGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);
    }

    /// <summary>
    /// Verifica condições de derrota (queda) e fim de nível (e aciona PathVerifier/Câmera).
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
    /// Aplica aceleração/desaceleração ao movimento horizontal e espelha o sprite.
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
    /// Define a orientação visual do sprite com base na direção e gravidade.
    /// </summary>
    private void Flip()
    {
        if (Mathf.Abs(horizontalInput) < 0.1f) return;
        bool wantsToGoLeft = horizontalInput < 0;
        spriteRenderer.flipX = wantsToGoLeft ^ IsGravityInverted;
    }

    /// <summary>
    /// Executa o salto quando solicitado e em contato com o chão.
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
    /// Inverte a gravidade e rotaciona o personagem quando solicitado e em solo.
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
    /// Ativa a UI de Game Over e desativa o objeto do jogador.
    /// </summary>
    private void PlayerDeath()
    {
        if (gameOverUI != null) gameOverUI.SetActive(true);
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Mata o jogador ao colidir com inimigos.
    /// </summary>
    /// <param name="collision">Dados da colisão 2D.</param>
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
    /// Atualiza parâmetros de animação (correr, cair, velocidade do clip de corrida).
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
    /// Desenha o gizmo de verificação de chão na cena quando selecionado.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
    }
}