using System;
using Unity.Android.Gradle.Manifest;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class PlayerMovementNew : MonoBehaviour
{
    [Header("References")]
    public PlayerMovementSTats movementSTats;
    [SerializeField] private Collider _feetColl;
    [SerializeField] private Collider _bodyColl;

    private Rigidbody _rb;

    // Movement variables
    private Vector3 _moveVelocity;
    private bool _isFacingRight;

    // Collision check variables
    private RaycastHit _groundHit;
    private bool _isGrounded;

    // Jump variables
    public float VerticalVelocity { get; private set; }
    private bool _isJumping;
    private bool _isFastFalling;
    private bool _isFalling;
    private float _fastFallTime;
    private float _fastFallReleaseSpeed;
    private int _numberOfJumpsUsed;
    



    // Apex variables
    private float _apexPoint;
    private float _timePastApexThreshold;
    private bool _isPastApexThreshold;

    // Jump buffer variables
    private float _jumpBufferTimer;
    private bool _jumpReleasedDuringBuffer;

    // Coyote time variables
    private float _coyoteTimer;
    private RaycastHit _headHit;
    private bool _bumpedHead;

    private void Awake()
    {
        _isFacingRight = true;
        _rb = GetComponent<Rigidbody>();

        // Freeze rotation on all axes except Y (optional)
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void Update()
    {
        CountTimers();
        JumpChecks();
    }

    private void FixedUpdate()
    {
        CollisionChecks();
        Jump();

        Vector3 input = new Vector3(InputManagerNew.Movement.x, 0f, 0f);

        if (_isGrounded)
        {
            Move(movementSTats.GroundAcceleration, movementSTats.GroundDeceleration, input);
        }
        else
        {
            Move(movementSTats.AirAcceleration, movementSTats.AirDeceleration, input);
        }
    }

    #region Movement

    

    [Obsolete]
    private void Move(float acceleration, float deceleration, Vector3 moveInput)
    {
        if (moveInput != Vector3.zero)
        {
            TurnCheck(moveInput);

            Vector3 targetVelocity = InputManagerNew.RunIsHeld ?
                moveInput * movementSTats.MaxRunSpeed :
                moveInput * movementSTats.MaxWalkSpeed;

            _moveVelocity = Vector3.Lerp(_moveVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
            _rb.velocity = new Vector3(_moveVelocity.x, _rb.velocity.y, 0f); // lock Z axis
        }
        else
        {
            _moveVelocity = Vector3.Lerp(_moveVelocity, Vector3.zero, deceleration * Time.fixedDeltaTime);
            _rb.velocity = new Vector3(_moveVelocity.x, _rb.velocity.y, 0f);
        }
    }

    private void TurnCheck(Vector3 moveInput)
    {
        if (_isFacingRight && moveInput.x < 0)
        {
            Turn(false);
        }
        else if (!_isFacingRight && moveInput.x > 0)
        {
            Turn(true);
        }
    }

    private void Turn(bool turnRight)
    {
        _isFacingRight = turnRight;
        transform.Rotate(0f, 180f, 0f); // Flip sprite visually
    }

    #endregion

    #region Jump

    private void JumpChecks()
    {

        //WHEN WE PRESS THE JUMP BUTTON
        if (InputManagerNew.JumpWasPressed)
        {
            _jumpBufferTimer = movementSTats.JumpBufferTime;
            _jumpReleasedDuringBuffer = false;

        }
        
        //WHEN WE RELEASE THE JUMP BUTTON
        if(InputManagerNew.JumpWasReleased)
        {
            if (_jumpBufferTimer > 0f)
            {
                _jumpReleasedDuringBuffer = true;
            }
            if (_isJumping && VerticalVelocity > 0f)
            {
                if(_isPastApexThreshold)
                {
                    _isPastApexThreshold = false;
                    _isFastFalling = true;
                    _fastFallTime = movementSTats.TimeForUpwardsCancel;
                    VerticalVelocity = 0f;
                }
                else
                {
                    _isFastFalling = true;
                    _fastFallReleaseSpeed = VerticalVelocity;
                }
            }
        }

        //INITIATE JUMP WITH JUMP BUFFERING AND COYOTE TIME
        if (_jumpBufferTimer > 0f && !_isJumping && (_isGrounded || _coyoteTimer > 0f))
        {
            InitiateJump(1);

            if (_jumpReleasedDuringBuffer) 
            {
                _isFastFalling = true;
                _fastFallReleaseSpeed = VerticalVelocity;

            }
        }

        //DOUBLE JUMP
        else if (_jumpBufferTimer > 0f && _isJumping && _numberOfJumpsUsed < movementSTats.NumberOfJumpsAllowed)
        {
            _isFastFalling = false;
            InitiateJump(1);
        }

        //AIR JUMP AFTER COYOTE TIME LAPSED
        else if (_jumpBufferTimer > 0f && _isFalling && _numberOfJumpsUsed < movementSTats.NumberOfJumpsAllowed -1)
        {
            InitiateJump(2); // I CAN ADD JUMP VFX HERE
            _isFastFalling = false;
            
        }

        //LANDED
        if ((_isJumping || _isFalling) && _isGrounded && VerticalVelocity <= 0f)
        {
            _isJumping = false;
            _isFalling = false;
            _isFastFalling = false;
            _fastFallTime = 0f;
            _isPastApexThreshold = false;
            _numberOfJumpsUsed = 0;

            VerticalVelocity = 0f;
        }

    }



    private void InitiateJump(int numberOfJumpsUsed)
    {
        if(!_isJumping)
        {
            _isJumping = true;
        }

        _jumpBufferTimer= 0f;
        _numberOfJumpsUsed += numberOfJumpsUsed;
        VerticalVelocity = movementSTats.InitialJumpVelocity;
    }

    private void Jump()
    {
        //APPLY GRAVITY WHILE JUMPING 
        if (_isJumping)
        {
            //CHECK FOR HEAD BUMP
            if (_bumpedHead)
            {
                _isFastFalling = true;
            }

            //GRAVITY ON ASCENDING
            if (VerticalVelocity >= 0f)
            {
                //APEX CONTROLS
                _apexPoint = Mathf.InverseLerp(movementSTats.InitialJumpVelocity, 0f, VerticalVelocity);

                if (_apexPoint > movementSTats.ApexThreshold)
                {
                    if(!_isPastApexThreshold)
                    {
                        _isPastApexThreshold = true;
                        _timePastApexThreshold = 0f;
                    }

                    if (_isPastApexThreshold)
                    {
                        _timePastApexThreshold += Time.fixedDeltaTime;
                        if (_timePastApexThreshold < movementSTats.ApexHangTime)
                        {
                            VerticalVelocity = 0f;
                        }
                        else
                        {
                            VerticalVelocity = -0.01f;
                        }
                    }
                }

                //GRAVITY ON ASCENDING BUT NOT PAST APEX THRESHOLD
                else
                {
                    VerticalVelocity += movementSTats.Gravity * Time.fixedDeltaTime;
                    if (_isPastApexThreshold)
                    {
                        _isPastApexThreshold = false;
                    }

                }

            }
            
            //GRAVITY ON DESCENDING 
            else if (!_isFastFalling)
            {
                VerticalVelocity += movementSTats.Gravity * movementSTats.GravityOnReleaseMultiplier * Time.fixedDeltaTime;
            }

            else if (VerticalVelocity < 0f)
            {
                if (!_isFalling)
                {
                    _isFalling = true;
                }
            }
        }
      

        //JUMP CUT
        if (_isFastFalling)
        {
            if (_fastFallTime >= movementSTats.TimeForUpwardsCancel)
            {
                VerticalVelocity += movementSTats.Gravity * movementSTats.GravityOnReleaseMultiplier * Time.fixedDeltaTime;
            }
            else if (_fastFallTime < movementSTats.TimeForUpwardsCancel)
            {
                VerticalVelocity = Mathf.Lerp(_fastFallReleaseSpeed, 0f, (_fastFallTime / movementSTats.TimeForUpwardsCancel));
            }
            _fastFallTime += Time.fixedDeltaTime;
        }

        //NORMAL GRAVITY WHILE FALLING
        if (!_isGrounded && !_isJumping)
        {
            if (!_isFalling)
            {
                _isFalling = true;
            }
            VerticalVelocity += movementSTats.Gravity * Time.fixedDeltaTime;
        }

        //CLAMP FALL SPEED
        VerticalVelocity = Mathf.Clamp(VerticalVelocity, -movementSTats.MaxFallSpeed, 50f); //can change the 50
        _rb.linearVelocity = new Vector3 (_rb.linearVelocity.x, VerticalVelocity, _rb.linearVelocity.z);
    }

    #endregion

    #region Collision Checks

    private void IsGrounded()
    {
        Vector3 origin = new Vector3(_feetColl.bounds.center.x, _feetColl.bounds.min.y - 0.01f, _feetColl.bounds.center.z);
        Vector3 halfExtents = new Vector3(_feetColl.bounds.extents.x, 0.05f, _feetColl.bounds.extents.z);
        float distance = movementSTats.GroundDetectionRayLength = 0.2f;

        _isGrounded = Physics.BoxCast(origin, halfExtents, Vector3.down, out _groundHit, Quaternion.identity, distance, movementSTats.GroundLayer);

        #region Debug Visualization

        if (movementSTats.DebugShowIsGroundedBox)
        {
            Color rayColor = _isGrounded ? Color.green : Color.red;
            Debug.DrawRay(origin, Vector3.down * distance, rayColor);
        }

        #endregion
    }




    private void BumpedHead()
{
    Vector3 boxCastOrigin = new Vector3(_feetColl.bounds.center.x, _bodyColl.bounds.max.y, _feetColl.bounds.center.z);
    Vector3 boxCastSize = new Vector3(_feetColl.bounds.size.x * movementSTats.HeadWidth, 0.1f, _feetColl.bounds.size.z);

    float castDistance = movementSTats.HeadDetectionRayLength;
    _bumpedHead = Physics.BoxCast(boxCastOrigin, boxCastSize * 0.5f, Vector3.up, out _headHit, Quaternion.identity, castDistance, movementSTats.GroundLayer);

    #region Debug Visualization

    if (movementSTats.DebugShowHeadBumpBox)
    {
        Color rayColor = _bumpedHead ? Color.green : Color.red;

        Vector3 offset = new Vector3(boxCastSize.x / 2 * movementSTats.HeadWidth, 0f, 0f);
        Vector3 rayStartL = boxCastOrigin - offset;
        Vector3 rayStartR = boxCastOrigin + offset;

        Debug.DrawRay(rayStartL, Vector3.up * castDistance, rayColor);
        Debug.DrawRay(rayStartR, Vector3.up * castDistance, rayColor);
        Debug.DrawRay(rayStartL + Vector3.up * castDistance, Vector3.right * boxCastSize.x * movementSTats.HeadWidth, rayColor);
    }

    #endregion
}















    private void CollisionChecks()
    {
        IsGrounded();
        BumpedHead();
    }

    #endregion

    #region Timers

    private void CountTimers()
    {
        _jumpBufferTimer -= Time.deltaTime;

        if (!_isGrounded)
        {
            _coyoteTimer -= Time.deltaTime;
        }
        else
        {
            _coyoteTimer = movementSTats.JumpCoyoteTime;
        }
    }

    #endregion
}
