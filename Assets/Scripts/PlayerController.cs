using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("MOVEMENT SETTINGS")]
    public float jumpForce = 7.0f;
    [SerializeField] private float xSpeed = 5.0f;
    [SerializeField] private bool isGround = false;
    [SerializeField] private Transform isGroundCheck;
    [SerializeField] private LayerMask isGroundLayer;
    [SerializeField] private bool run;
    [SerializeField] private bool jump;

    [Header("COMPONENT REFERENCES")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private AudioSource sound;

    [Header("LIFE SYSTEM")]
    [SerializeField] private int life = 3;
    [SerializeField] private Transform life1;
    [SerializeField] private Transform life2;
    [SerializeField] private Transform life3;

    [Header("LEVEL PROGRESS")]
    [SerializeField] private Transform levelClear;
    [SerializeField] private Transform gameOver;



    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        gameOver.gameObject.SetActive(false);
        levelClear.gameObject.SetActive(false);
        sound = GetComponent<AudioSource>();
    }

    void Update()
    {
        jump = false;

        //Verifica se o jogador está no chão;
        isGround = Physics2D.OverlapCircle(isGroundCheck.position, 0.5f, isGroundLayer);

        //Movimenta o jogador horizontalmente
        float xInput =Input.GetAxis("Horizontal");
        rb.velocity = new Vector2(xInput * xSpeed, rb.velocity.y);

        run = Mathf.Abs(xInput) > 0.3;


        //Inverte a direção do sprite
        if(jumpForce > 0){
            if(xInput > 0)
                transform.eulerAngles = new Vector3(0f, 0f, 0f);
            else if(xInput < 0)
                transform.eulerAngles = new Vector3(0f, 180f, 0f);
        }
        else if(jumpForce < 0){
            if(xInput > 0)
                transform.eulerAngles = new Vector3(180f, 0f, 0f);
            else if(xInput < 0)
                transform.eulerAngles = new Vector3(180f, 180f, 0f);
        }

        //Verifica se o jogador está no chão e se o botão de pulo foi pressionado
        if (Input.GetKeyDown(KeyCode.W) && isGround)
        {
            rb.velocity = new Vector2(0, jumpForce);
            jump = true;
            jumpForce = -jumpForce; // Inverte a direção do pulo
            rb.gravityScale = -rb.gravityScale; // Inverte a gravidade do Rigidbody2D
        }
        else
        {
            // reset da flag de pulo para a animação
            jump = false;
        }

                //Verifica se o jogador está no chão e se o botão de pulo foi pressionado
        if (Input.GetKeyDown(KeyCode.Space) && isGround)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            jump = true;
        }
        else
        {
            // reset da flag de pulo para a animação
            jump = false;
        }


        animator.SetBool("Running", run);
        animator.SetBool("Jumping", jump);

        // Debug.Log("FPS: " + (1.0f / Time.deltaTime));

        Application.targetFrameRate = 60;

        if(transform.position.y < -25 || transform.position.y > 25)
        {
            PlayerDeath();
        }

        if (transform.position.x > 25)
        {
            levelClear.gameObject.SetActive(true);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Enemy"))
        {
            sound.Play();
            if(life == 3)
            {
                life3.gameObject.SetActive(false);
                life--;
            }
            else if(life == 2)
            {
                life2.gameObject.SetActive(false);
                life--;
            }
            else if(life == 1)
            {
                life1.gameObject.SetActive(false);
                life--;
                PlayerDeath();
            }

        }
    }

    void PlayerDeath()
    {
        Destroy(gameObject);
        gameOver.gameObject.SetActive(true);
    }

    public void DestroyPlayer()
    {
        Destroy(gameObject);
    }
}