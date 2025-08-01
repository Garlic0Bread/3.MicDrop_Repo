using System;
using System.Collections;
//using Unity.Android.Gradle.Manifest;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class Player2MovementNew : MonoBehaviour
{
    [Header("References")]
    public PlayerMovementSTats movementSTats;
    [SerializeField] private Collider _feetColl;
    [SerializeField] private Collider _bodyColl;
    [SerializeField] private int _playerIndex = 0; // Added player index
    public Combat combat;

    private KnockbackReceiver knockbackReceiver;



    private Rigidbody _rb;
    private InputManagerNew _inputManager;
    private PlayerInput playerInput;
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


    public bool IsInHitStun { get; private set; }

    [Header("Side Step Settings")]
    // General Side Step Variables
    public int _sideStepAmount = 20; // Amount of units the player side steps along the z-axis

    // Forward Side Step Variables
    private Vector3 _originalPosition_forward; // Stores the original position before forward side step
    private Vector3 _sideStepPosition_forward; // Stores the position after forward side step

    // Backward Side Step Variables
    private Vector3 _originalPosition_backward; // Stores the original position before backward side step
    private Vector3 _sideStepPosition_backward; // Stores the position after backward side step


    [Obsolete]
    private void Awake()
    {
        //combat = GetComponent<Combat>();

        _isFacingRight = true;
        _rb = GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.FreezeRotation;



        knockbackReceiver = GetComponent<KnockbackReceiver>();

        // Initialize input manager reference
        _inputManager = InputManagerNew.GetByPlayerIndex(_playerIndex);
        if (_inputManager == null)
        {
            Debug.LogError($"No InputManager found for player {_playerIndex}");
        }
    }


    private void Update()
    {
        if (_inputManager == null || IsInHitStun) return; // Skip input if in hit stun


        if (_inputManager == null) return;

        CountTimers();
        JumpChecks();

        // Handle dash input through input manager
        if (_inputManager.PlayerInput.actions["Dash"].WasPressedThisFrame())
        {
            TryDash();
        }

        if (_inputManager.AttackPressed)
        {
            combat.StartAttack();
        }

        // Side Step Input Handling
        if (_inputManager.PlayerInput.actions["Side Step Forward"].WasPressedThisFrame())
        {
            Debug.Log("Side Step Forward Pressed");
            TrySideStep_Forward();
        }

        if (_inputManager.PlayerInput.actions["Side Step Backward"].WasPressedThisFrame())
        {
            Debug.Log("Side Step Backward Pressed");
            TrySideStep_Backward();
        }

        // Forward Side Step Position Calculation
        _originalPosition_forward = this.gameObject.transform.position; // Store the original position for side step

        _sideStepPosition_forward = _originalPosition_forward + new Vector3(0f, 0f, +_sideStepAmount); // Calculate the side step position

        // Backward Side Step Position Calculation
        _originalPosition_backward = this.gameObject.transform.position; // Store the original position for side step

        _sideStepPosition_backward = _originalPosition_backward + new Vector3(0f, 0f, -_sideStepAmount); // Calculate the side step position

        //transform.position = new Vector3(transform.position.x, transform.position.y, -90); // Keep characters locked to 2D plane.. NOT BEING USED ANYMORE
    }

    private void FixedUpdate()
    {
        if (_inputManager == null || IsInHitStun) return; // Skip physics if in hit stun


        if (_inputManager == null) return;

        CollisionChecks();
        Jump();

        Vector3 input = new Vector3(_inputManager.Movement.x, 0f, 0f);

        if (_isGrounded)
        {
            Move(movementSTats.GroundAcceleration, movementSTats.GroundDeceleration, input);
        }
        else
        {
            Move(movementSTats.AirAcceleration, movementSTats.AirDeceleration, input);
        }

    }









    #region Combat











    public void OnHit()
    {
        combat.CancelAttack();
        if (knockbackReceiver != null)
        {
            knockbackReceiver.ApplyHitStun(knockbackReceiver.hitStunDuration);
        }
        // Other hit reaction code...
    }



    #endregion




    #region Movement

    private void Move(float acceleration, float deceleration, Vector3 moveInput)
    {
        if (_isDashing) return;

        if (moveInput != Vector3.zero)
        {
            TurnCheck(moveInput);

            Vector3 targetVelocity = moveInput * movementSTats.MaxWalkSpeed;
            _moveVelocity = Vector3.Lerp(_moveVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
            _rb.linearVelocity = new Vector3(_moveVelocity.x, _rb.linearVelocity.y, 0f);
        }
        else
        {
            _moveVelocity = Vector3.Lerp(_moveVelocity, Vector3.zero, deceleration * Time.fixedDeltaTime);
            _rb.linearVelocity = new Vector3(_moveVelocity.x, _rb.linearVelocity.y, 0f);
        }
    }

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

        float currentYVelocity = _rb.linearVelocity.y;
        _rb.linearVelocity = new Vector3(
            direction.x * _dashSpeed,
            currentYVelocity,
            0f
        );

        yield return new WaitForSeconds(_dashDuration);

        _rb.linearVelocity = new Vector3(
            _rb.linearVelocity.x * 0.3f,
            _rb.linearVelocity.y,
            0f
        );

        _isDashing = false;
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
        transform.Rotate(0f, 180f, 0f);
    }

    private void TrySideStep_Forward()
    {
        this.gameObject.transform.position = _sideStepPosition_forward; // Move player to side step position   
    }

    private void TrySideStep_Backward()
    {
        this.gameObject.transform.position = _sideStepPosition_backward; // Move player to side step position   
    }

    #endregion

    #region Jump

    private void JumpChecks()
    {
        if (_inputManager.JumpWasPressed)
        {
            _jumpBufferTimer = movementSTats.JumpBufferTime;
            _jumpReleasedDuringBuffer = false;
        }

        if (_inputManager.JumpWasReleased)
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

        if (_jumpBufferTimer > 0f && !_isJumping && (_isGrounded || _coyoteTimer > 0f))
        {
            InitiateJump(1);

            if (_jumpReleasedDuringBuffer)
            {
                _isFastFalling = true;
                _fastFallReleaseSpeed = VerticalVelocity;
            }
        }
        else if (_jumpBufferTimer > 0f && _isJumping && _numberOfJumpsUsed < movementSTats.NumberOfJumpsAllowed)
        {
            _isFastFalling = false;
            InitiateJump(1);
        }
        else if (_jumpBufferTimer > 0f && _isFalling && _numberOfJumpsUsed < movementSTats.NumberOfJumpsAllowed - 1)
        {
            InitiateJump(2);
            _isFastFalling = false;
        }

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
        if (_isJumping)
        {
            if (_bumpedHead)
            {
                _isFastFalling = true;
            }

            if (VerticalVelocity >= 0f)
            {
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
                else
                {
                    VerticalVelocity += movementSTats.Gravity * Time.fixedDeltaTime;
                    if (_isPastApexThreshold)
                    {
                        _isPastApexThreshold = false;
                    }
                }
            }
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

        if (!_isGrounded && !_isJumping)
        {
            if (!_isFalling)
            {
                _isFalling = true;
            }
            VerticalVelocity += movementSTats.Gravity * Time.fixedDeltaTime;
        }

        VerticalVelocity = Mathf.Clamp(VerticalVelocity, -movementSTats.MaxFallSpeed, 50f);
        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, VerticalVelocity, _rb.linearVelocity.z);
    }

    #endregion

    #region Collision Checks

    private void IsGrounded()
    {
        Vector3 origin = new Vector3(_feetColl.bounds.center.x, _feetColl.bounds.min.y - 0.01f, _feetColl.bounds.center.z);
        Vector3 halfExtents = new Vector3(_feetColl.bounds.extents.x * movementSTats.HeadWidth, 0.1f, _feetColl.bounds.extents.z);
        float distance = movementSTats.GroundDetectionRayLength;

        // Original ground check
        _isGrounded = Physics.BoxCast(origin, halfExtents * 0.5f, Vector3.down, out _groundHit,
                                    Quaternion.identity, distance, movementSTats.GroundLayer);

        // Additional check if no vertical movement
        if (!_isGrounded && Mathf.Abs(_rb.linearVelocity.y) < 0.01f)
        {
            _isGrounded = Physics.CheckBox(origin + Vector3.down * distance * 0.5f,
                                         halfExtents * 0.5f,
                                         Quaternion.identity,
                                         movementSTats.GroundLayer);
        }

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
    }

    private void BumpedHead()
    {
        // (Keep original BumpedHead implementation unchanged)
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
