using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Animator animator;
    public float xSpeed = 5.0f;
    public float jumpForce = 7.0f;
    private Rigidbody2D rb;
    private bool isGround = false;
    public Transform isGroundCheck;
    public LayerMask isGroundLayer;
    private bool run;
    private bool jump;
    public Transform LevelClear;
    public int life = 3;
    public Transform Life1;
    public Transform Life2;
    public Transform Life3;
    private AudioSource sound;




    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        //GameOver.gameObject.SetActive(false);
        //LevelClear.gameObject.SetActive(false);
        sound = GetComponent<AudioSource>();
    }

    void Update()
    {
        jump = false;
        
        

        //Primeiro verifica se o jogador está no chão;
        isGround = Physics2D.OverlapCircle(isGroundCheck.position, 0.5f, isGroundLayer);

        //Movimenta o jogador horizontalmente
        float xInput = Input.GetAxis("Horizontal");
        rb.velocity = new Vector2(xInput * xSpeed, rb.velocity.y);

        run = Mathf.Abs(xInput) > 0.3f;


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
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
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

        if(transform.position.y < -25)
        {
            PlayerDeath();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Enemy"))
        {
            sound.Play();
            if(life == 3)
            {
                Life3.gameObject.SetActive(false);
                life--;
            }
            else if(life == 2)
            {
                Life2.gameObject.SetActive(false);
                life--;
            }
            else if(life == 1)
            {
                Life1.gameObject.SetActive(false);
                life--;
                PlayerDeath();
            }

        }
    }

    void PlayerDeath()
    {
        Destroy(gameObject);
        //GameOver.gameObject.SetActive(true);
    }

    public void DestroyPlayer()
    {
        Destroy(gameObject);
    }
}