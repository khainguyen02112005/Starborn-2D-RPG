using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    [Header("Dash")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;

    private bool isDashing;
    private float dashCounter;
    private Rigidbody2D rb;
    private Vector2 movement;
    private PlayerHealth playerHealth;
    private PlayerSPUMAnimation spumAnimation;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerHealth = GetComponent<PlayerHealth>();
        spumAnimation = GetComponent<PlayerSPUMAnimation>();
    }

    private void Update()
    {
        if (GameplayUiFocus.BlocksControls)
        {
            isDashing = false; movement = Vector2.zero;
            if (rb != null) rb.linearVelocity = Vector2.zero;
            spumAnimation?.SetMoving(false);
            return;
        }
        if (playerHealth != null && playerHealth.IsDead)
        {
            isDashing = false;
            movement = Vector2.zero;
            spumAnimation?.SetMoving(false);
            return;
        }

        if (isDashing)
        {
            dashCounter -= Time.deltaTime;
            if (dashCounter <= 0f)
            {
                isDashing = false;
            }

            spumAnimation?.SetMoving(movement.sqrMagnitude > 0f);
            return;
        }

        float moveX = 0f;
        float moveY = 0f;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveY = 1f;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveY = -1f;

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveX = 1f;
        else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveX = -1f;

        movement = new Vector2(moveX, moveY).normalized;
        spumAnimation?.SetMoving(movement.sqrMagnitude > 0f);

        if (movement.x > 0f)
        {
            Vector3 scale = transform.localScale;
            scale.x = -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
        else if (movement.x < 0f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && movement.sqrMagnitude > 0f)
        {
            PlayerStamina stamina = GetComponent<PlayerStamina>();
            if (stamina != null && stamina.UseStamina(30f))
            {
                isDashing = true;
                dashCounter = dashDuration;
                GameAudioManager.Play(GameSound.Dash, transform.position, 0.72f, 1.05f);
            }
        }
    }

    private void FixedUpdate()
    {
        if (rb == null || (playerHealth != null && playerHealth.IsDead))
        {
            return;
        }

        float speed = isDashing ? dashSpeed : moveSpeed;
        rb.MovePosition(rb.position + movement * speed * Time.fixedDeltaTime);
    }

    private void OnDisable()
    {
        isDashing = false; movement = Vector2.zero;
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }
}
