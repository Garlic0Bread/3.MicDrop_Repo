using System;
using System.Collections;
using Unity.Android.Gradle.Manifest;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
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

    [Header("Dash Settings")]
    [SerializeField] private float _dashSpeed = 20f;  
    [SerializeField] private float _dashDuration = 0.2f; 
    [SerializeField] private float _dashCooldown = 0.5f;
    private bool _isDashing;
    private float _lastDashTime;


    // Collision check variables
    private RaycastHit _groundHit;
    public bool _isGrounded;

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
    
    // Direct keyboard input check - works without Input System
    if (Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame)
    {
        TryDash();
    }
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

         // Direct keyboard input check - works without Input System
        if (UnityEngine.InputSystem.Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame)
        {
            TryDash();
        }







    }


    private void OnDrawGizmos()
    {
        if (movementSTats.ShowWalkJumpArc)
        {
            DrawJumpArc(movementSTats.MaxWalkSpeed, Color.white);
        }
        if (movementSTats.ShowRunJumpArc)
        {
            DrawJumpArc(movementSTats.MaxRunSpeed, Color.red);
        }

    }

    
#region Movement



private void Move(float acceleration, float deceleration, Vector3 moveInput)
{
    if (_isDashing) return; // Skip movement during dash

    if (moveInput != Vector3.zero)
    {
        TurnCheck(moveInput);

        // Normal walking (no run)
        Vector3 targetVelocity = moveInput * movementSTats.MaxWalkSpeed;
        _moveVelocity = Vector3.Lerp(_moveVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
        _rb.linearVelocity = new Vector3(_moveVelocity.x, _rb.linearVelocity.y, 0f); // Lock Z-axis
    }
    else
    {
        _moveVelocity = Vector3.Lerp(_moveVelocity, Vector3.zero, deceleration * Time.fixedDeltaTime);
        _rb.linearVelocity = new Vector3(_moveVelocity.x, _rb.linearVelocity.y, 0f);
    }
}

// Call this from input (e.g., when "Run" button is pressed)
public void TryDash()
{
    if (Time.time < _lastDashTime + _dashCooldown || _isDashing) 
        return;

    Vector3 dashDirection = _isFacingRight ? Vector3.right : Vector3.left;
    StartCoroutine(DashRoutine(dashDirection));
}

private IEnumerator DashRoutine(Vector3 direction)
{
    _isDashing = true;
    _lastDashTime = Time.time;
    
    // Store current Y velocity before dashing
    float currentYVelocity = _rb.linearVelocity.y;
    
    // Apply dash only to X axis
    _rb.linearVelocity = new Vector3(
        direction.x * _dashSpeed,
        currentYVelocity,  // Maintain existing vertical velocity
        0f
    );

    yield return new WaitForSeconds(_dashDuration);
    
    // Smooth dash exit (optional)
    _rb.linearVelocity = new Vector3(
        _rb.linearVelocity.x * 0.3f,  // Reduce x velocity after dash
        _rb.linearVelocity.y,
        0f
    );
    
    _isDashing = false;
}

// Keep TurnCheck/Turn methods unchanged
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
        if (InputManagerNew.JumpWasReleased)
        {
            if (_jumpBufferTimer > 0f)
            {
                _jumpReleasedDuringBuffer = true;
            }
            if (_isJumping && VerticalVelocity > 0f)
            {
                if (_isPastApexThreshold)
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
        else if (_jumpBufferTimer > 0f && _isFalling && _numberOfJumpsUsed < movementSTats.NumberOfJumpsAllowed - 1)
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
        if (!_isJumping)
        {
            _isJumping = true;
        }

        _jumpBufferTimer = 0f;
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
                    if (!_isPastApexThreshold)
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
        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, VerticalVelocity, _rb.linearVelocity.z);
    }

    #endregion

    #region Collision Checks

    private void IsGrounded()
    {
        Vector3 origin = new Vector3(_feetColl.bounds.center.x, _feetColl.bounds.min.y - 0.01f, _feetColl.bounds.center.z);
        Vector3 halfExtents = new Vector3(_feetColl.bounds.extents.x * movementSTats.HeadWidth, 0.1f, _feetColl.bounds.extents.z);
        float distance = movementSTats.GroundDetectionRayLength = 0.9f;


        _isGrounded = Physics.BoxCast(origin, halfExtents * 0.5f, Vector3.down, out _groundHit, Quaternion.identity, distance, movementSTats.GroundLayer);

        #region Debug Visualization

        if (movementSTats.DebugShowIsGroundedBox)
        {
            Color rayColor = _isGrounded ? Color.green : Color.red;
            Vector3 offset = new Vector3(halfExtents.x / 2 * movementSTats.HeadWidth, 0f, 0f);
            Vector3 rayStartL = origin - offset;
            Vector3 rayStartR = origin + offset;

            Debug.DrawRay(rayStartL, Vector3.down * distance, rayColor);
            Debug.DrawRay(rayStartR, Vector3.down * distance, rayColor);
            Debug.DrawRay(rayStartL + Vector3.down * distance, Vector3.right * halfExtents.x * movementSTats.HeadWidth, rayColor);
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



    #region Gizmos


    private void DrawJumpArc(float moveSpeed, Color gizmoColor)
    {
        Vector3 startPosition = new Vector3(_feetColl.bounds.center.x, _feetColl.bounds.min.y, _feetColl.bounds.center.z);
        Vector3 previousPosition = startPosition;
        float speed = 0f;

        if (movementSTats.DrawRight)
        {
            speed = moveSpeed;
        }
        else
        {
            speed = -moveSpeed;
        }

        Vector3 velocity = new Vector3(speed, movementSTats.InitialJumpVelocity, 0f);

        Gizmos.color = gizmoColor;

        float timeStep = movementSTats.TimeTillJumpApex / movementSTats.ArcResolution;

        for (int i = 0; i < movementSTats.VisualizationSteps; i++)
        {
            float simulationTime = i * timeStep;
            Vector3 displacement;
            Vector3 drawPoint;

            if (simulationTime < movementSTats.TimeTillJumpApex) // Ascending
            {
                displacement = velocity * simulationTime + 0.5f * new Vector3(0, movementSTats.Gravity, 0) * simulationTime * simulationTime;
            }
            else if (simulationTime < movementSTats.TimeTillJumpApex + movementSTats.ApexHangTime) // Apex hang time
            {
                float apexTime = movementSTats.TimeTillJumpApex;
                displacement = velocity * apexTime + 0.5f * new Vector3(0, movementSTats.Gravity, 0) * apexTime * apexTime;
                displacement += new Vector3(speed, 0f, 0f) * (simulationTime - apexTime); // Horizontal movement only during hang time
            }
            else // Descending
            {
                float descendTime = simulationTime - (movementSTats.TimeTillJumpApex + movementSTats.ApexHangTime);
                Vector3 apexDisplacement = velocity * movementSTats.TimeTillJumpApex + 0.5f * new Vector3(0, movementSTats.Gravity, 0) * movementSTats.TimeTillJumpApex * movementSTats.TimeTillJumpApex;
                Vector3 hangDisplacement = new Vector3(speed, 0f, 0f) * movementSTats.ApexHangTime;
                Vector3 descendDisplacement = new Vector3(speed, 0f, 0f) * descendTime + 0.5f * new Vector3(0, movementSTats.Gravity, 0) * descendTime * descendTime;

                displacement = apexDisplacement + hangDisplacement + descendDisplacement;
            }

            drawPoint = startPosition + displacement;

            if (movementSTats.StopOnCollision)
            {
                RaycastHit hit;
                if (Physics.Raycast(previousPosition, drawPoint - previousPosition, out hit, Vector3.Distance(previousPosition, drawPoint), movementSTats.GroundLayer))
                {
                    Gizmos.DrawLine(previousPosition, hit.point);
                    break;
                }
            }

            Gizmos.DrawLine(previousPosition, drawPoint);
            previousPosition = drawPoint;
        }
    }






    #endregion










}
