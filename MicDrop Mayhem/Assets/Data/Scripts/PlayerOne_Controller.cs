using OWL;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerOne_Controller : MonoBehaviour
{
    public float verticalVelocity { get; private set; }
    private Coroutine resetTriggersCoroutine;

    public float KBForce;
    public float KBCounter;
    public float KBTotalTime;
    public bool KnockFromRight;
    public bool playerCanMove;

    [Header("References")]
    [SerializeField] private Collider2D feetCollider;
    [SerializeField] private Collider2D bodyCollider;
    public PlayerMovemement_Stats moveStats;
    private Rigidbody2D rb2;
    private Animator anim;

    [Header("Jump Variables")]
    [SerializeField] private float jumpUpwardForce;
    [SerializeField] private float jumpAwayForce;
    private float fastFallReleasesSpeed;
    private int numberOfJumpsUsed;
    private bool isFastFalling;
    private float fastfallTime;
    private bool isJumping;
    private bool isFalling;

    [Header("Jump Buffer Variables")]
    private bool jumpReleasesDuringBuffer;
    private float jumpBufferTimer;
    private float coyoteTimer;
    private float timePastApexThreshold;
    private bool isPastApexThreshold;
    private float apexPoint;

    [Header("Collision & Movement Variables")]
    private RaycastHit2D groundHit;
    private RaycastHit2D wallHit;
    private RaycastHit2D headHit;
    private Vector2 moveVelocity;
    public bool isFacingRight;
    private bool isGrounded;
    private bool bumbedHead;

    private void Start()
    {
        isFacingRight = true;
        anim = GetComponent<Animator>();
        rb2 = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        JumpTimers();
        JumpChecks();
    }

    private void FixedUpdate()
    {
        CollisionChecks();
        HandleMovement();
        Jump();
    }

    private void CollisionChecks()
    {
        OnWall();
        IsGrounded();
        BumpedHead();
    }
    private void HandleMovement()
    {
        if (KBCounter <= 0)
        {
            float acceleration = isGrounded ? moveStats.GroundAcceleration : moveStats.AirAcceleration;
            float deceleration = isGrounded ? moveStats.GroundDeceleration : moveStats.AirDeceleration;
            Vector2 moveInput = _InputManager.P1_Movement;

            Move(acceleration, deceleration, moveInput);
        }
        else
        {
            ApplyKnockback(KBForce / 2);
            KBCounter -= Time.fixedDeltaTime;
        }
    }
    private void Jump()
    {
        ApplyJumpPhysics();
    }

    #region JUMPING
    private void ApplyJumpPhysics()
    {
        if (isJumping)
        {
            if (bumbedHead)
            {
                isFastFalling = true;
            }
            //gravity on ascending
            if (verticalVelocity >= 0f)
            {
                //apex controls
                apexPoint = Mathf.InverseLerp(moveStats.InitialJumpVelocity, 0f, verticalVelocity);

                if (apexPoint > moveStats.apexThreshold)
                {
                    if (!isPastApexThreshold)
                    {
                        isPastApexThreshold = true;
                        timePastApexThreshold = 0f;
                    }

                    if (isPastApexThreshold)
                    {
                        timePastApexThreshold += Time.fixedDeltaTime;
                        if (timePastApexThreshold < moveStats.apexHangTime)
                        {
                            verticalVelocity = 0f;
                        }
                        else
                        {
                            verticalVelocity = -0.01f;
                        }
                    }
                }
                //gravity on ascending but not pas apex threshold
                else
                {
                    verticalVelocity += moveStats.Gravity * Time.fixedDeltaTime;
                    if (isPastApexThreshold)
                    {
                        isPastApexThreshold = false;
                    }
                }
            }

            //gravity on descending
            else if (!isFastFalling)
            {
                verticalVelocity += moveStats.Gravity * moveStats.gravityOnReleaseMultiplier * Time.fixedDeltaTime;
            }
            else if (verticalVelocity < 0f)
            {
                if (!isFalling)
                {
                    isFalling = true;
                }
            }
        }

        if (isFastFalling)
        {
            anim.SetBool("canFall", true);

            if (fastfallTime >= moveStats.timeForUpwardsCancel)
            {
                verticalVelocity += moveStats.Gravity * moveStats.gravityOnReleaseMultiplier * Time.fixedDeltaTime;
            }
            else if (fastfallTime < moveStats.timeForUpwardsCancel)
            {
                verticalVelocity = Mathf.Lerp(fastFallReleasesSpeed, 0f, (fastfallTime / moveStats.timeForUpwardsCancel));
            }
            fastfallTime += Time.fixedDeltaTime;
        }

        //normal gravity while falling
        if (!isGrounded && !isJumping)
        {
            if (!isFalling)
            {
                isFalling = true;
            }
            verticalVelocity += moveStats.Gravity * Time.fixedDeltaTime;
        }

        //clamp fall speed
        verticalVelocity = Mathf.Clamp(verticalVelocity, -moveStats.maxFallSpeed, 50f);
        rb2.linearVelocity = new Vector2(rb2.linearVelocity.x, verticalVelocity);
    }
    private IEnumerator Reset()
    {
        yield return null;
        anim.ResetTrigger("Jump");
    }
    private void InitiateJump(int NumberOfJumpsUsed)
    {
        if (this.gameObject.CompareTag("Player1"))
        {
            if (!isJumping)
            {
                isJumping = true;
            }
            jumpBufferTimer = 0f;
            numberOfJumpsUsed += NumberOfJumpsUsed;
            verticalVelocity = moveStats.InitialJumpVelocity;
        }

        else if (this.gameObject.CompareTag("Player2"))
        {
            if (!isJumping)
            {
                isJumping = true;
            }
            jumpBufferTimer = 0f;
            numberOfJumpsUsed += NumberOfJumpsUsed;
            verticalVelocity = moveStats.InitialJumpVelocity;
        }
    }
    private void JumpChecks()
    {
        anim.SetFloat("AirY_Speed", rb2.linearVelocity.y);

        //when jump button is pressed
        if (_InputManager.P1_jumpWasPressed)
        {
            anim.SetTrigger("Jump");
            jumpBufferTimer = moveStats.jumpBufferTime;
            jumpReleasesDuringBuffer = false;
        }

        //when released
        if (_InputManager.P1_jumpWasReleased)
        {
            resetTriggersCoroutine = StartCoroutine(Reset());

            if (jumpBufferTimer > 0f)
            {
                jumpReleasesDuringBuffer = true;
            }
            if (isJumping && verticalVelocity > 0f)
            {
                if (isPastApexThreshold)
                {
                    isPastApexThreshold = false;
                    isFastFalling = true;
                    fastfallTime = moveStats.timeForUpwardsCancel;
                    verticalVelocity = 0f;
                }
                else
                {
                    isFastFalling = true;
                    fastFallReleasesSpeed = verticalVelocity;
                }
            }
        }

        //initiate jump w jump buffering and coyote time
        if (jumpBufferTimer > 0f && !isJumping && (isGrounded || coyoteTimer > 0f))
        {
            InitiateJump(1);
            if (jumpReleasesDuringBuffer)
            {
                isFastFalling = true;
                fastFallReleasesSpeed = verticalVelocity;
            }
        }

        //double jump
        else if (jumpBufferTimer > 0f && isJumping && numberOfJumpsUsed < moveStats.numberOfJumpsAllowed)
        {
            isFastFalling = false;
            InitiateJump(1);
        }

        //air jump after coyote time lapsed
        else if (jumpBufferTimer > 0f && isFalling && numberOfJumpsUsed < moveStats.numberOfJumpsAllowed - 1)
        {
            InitiateJump(2);
            isFastFalling = false;
        }

        //landed
        if ((isJumping || isFastFalling) && isGrounded && verticalVelocity <= 0f)
        {
            isJumping = false;
            isFalling = false;
            isFastFalling = false;
            fastfallTime = 0f;
            isPastApexThreshold = false;
            numberOfJumpsUsed = 0;

            verticalVelocity = Physics2D.gravity.y;
        }
        else if (OnWall() && !isGrounded)
        {
            print("ON WALL");
            if (_InputManager.P1_jumpWasPressed)
            {
                // Determine wall direction using isFacingRight (1 if facing right, -1 if facing left)
                float wallDirection = isFacingRight ? 1f : -1f;

                // Create a Vector2 for jumping away from the wall and upwards
                Vector2 jumpDirection = new Vector2(-wallDirection * jumpAwayForce, jumpUpwardForce);

                // Apply the jump direction to the player's Rigidbody2D
                moveVelocity = Vector2.Lerp(moveVelocity, jumpDirection, 20f * Time.fixedDeltaTime);
                rb2.linearVelocity = new Vector2(moveVelocity.x, rb2.linearVelocity.y);
                verticalVelocity = moveVelocity.magnitude;
            }
        }//wall jump
    }
    private void JumpTimers()
    {
        jumpBufferTimer -= Time.deltaTime;
        if (!isGrounded)
        {
            coyoteTimer -= Time.deltaTime;
        }
        else
        {
            coyoteTimer = moveStats.jumpCoyoteTime;
        }
    }
    #endregion

    #region MOVEMENT LOGIC
    private void Move(float acceleration, float deceleration, Vector2 moveInput)
    {
        if (!playerCanMove) return;

        anim.SetBool("isWalking", moveInput != Vector2.zero);

        if (moveInput != Vector2.zero)
        {
            TurnCheck(moveInput);
            Vector2 targetVelocity = new Vector2(moveInput.x, 0f) * GetMaxSpeed();
            moveVelocity = Vector2.Lerp(moveVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
        }
        else
        {
            moveVelocity = Vector2.Lerp(moveVelocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
        }

        rb2.linearVelocity = new Vector2(moveVelocity.x, rb2.linearVelocity.y);
    }

    private void ApplyKnockback(float knockbackY)
    {
        float knockbackX = KnockFromRight ? -KBForce : KBForce;
        rb2.linearVelocity = new Vector2(knockbackX, knockbackY);
    }

    private void TurnCheck(Vector2 moveInput)
    {
        if (isFacingRight && moveInput.x < 0)
            Turn(false);
        else if (!isFacingRight && moveInput.x > 0)
            Turn(true);
    }

    private void Turn(bool turnRight)
    {
        isFacingRight = turnRight;
        transform.Rotate(0f, turnRight ? 180f : -180f, 0f);
    }

    private float GetMaxSpeed()
    {
        return _InputManager.P1_runIsHeld ? moveStats.MaxRunSpeed : moveStats.MaxWalkSpeed;
    }
    #endregion

    #region COLLISION CHECKS
    private bool OnWall()
    {
        Vector2 boxCastOrigin = new Vector2(bodyCollider.bounds.center.x, bodyCollider.bounds.min.y);
        Vector2 boxCastSize = new Vector2(bodyCollider.bounds.size.x, moveStats.GroundDetectionRayLength);

        float direction = isFacingRight ? 1f : -1f;

        wallHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, new Vector2(direction, 0), moveStats.GroundDetectionRayLength, moveStats.WallLayer);
        return wallHit.collider != null;
    }
    private bool IsGrounded()
    {
        Vector2 boxCastOrigin = new Vector2(feetCollider.bounds.center.x, feetCollider.bounds.min.y);
        Vector2 boxCastSize = new Vector2(feetCollider.bounds.size.x, moveStats.GroundDetectionRayLength);

        groundHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.down, moveStats.GroundDetectionRayLength, moveStats.Groundlayer);
        if (groundHit.collider != null)
        {
            isGrounded = true;
            anim.SetBool("isGrounded", true);

        }
        else
        {
            isGrounded = false;
            anim.SetBool("isGrounded", false);
        }
        return groundHit.collider != null;
    }
    private void BumpedHead()
    {
        Vector2 boxCastOrigin = new Vector2(feetCollider.bounds.center.x, bodyCollider.bounds.max.y);
        Vector2 boxCastSize = new Vector2(feetCollider.bounds.size.x * moveStats.HeadWidth, moveStats.HeadDetectionRayLength);

        headHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.up, moveStats.HeadDetectionRayLength, moveStats.Groundlayer);
        if (headHit.collider != null)
        {
            bumbedHead = true;
        }
        else
        {
            bumbedHead = false;
        }
    }
    #endregion
}
