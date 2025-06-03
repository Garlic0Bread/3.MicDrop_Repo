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



        // Keep characters locked to 2D plane
        transform.position = new Vector3(transform.position.x, transform.position.y, -90);
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
    // BoxCast (original check)
    Vector3 boxOrigin = new Vector3(_feetColl.bounds.center.x, _feetColl.bounds.min.y - 0.01f, _feetColl.bounds.center.z);
    Vector3 halfExtents = new Vector3(_feetColl.bounds.extents.x * movementSTats.HeadWidth, 0.1f, _feetColl.bounds.extents.z);
    float distance = movementSTats.GroundDetectionRayLength;

    _isGrounded = Physics.BoxCast(boxOrigin, halfExtents * 0.5f, Vector3.down, out _groundHit, 
                                Quaternion.identity, distance, movementSTats.GroundLayer);

    // Add 3 Raycasts (left, center, right) for edge detection
    float raySpacing = _feetColl.bounds.extents.x * 0.8f; // Adjust spacing as needed
    Vector3 rayStartCenter = boxOrigin;
    Vector3 rayStartLeft = boxOrigin - new Vector3(raySpacing, 0, 0);
    Vector3 rayStartRight = boxOrigin + new Vector3(raySpacing, 0, 0);

    bool centerRay = Physics.Raycast(rayStartCenter, Vector3.down, distance, movementSTats.GroundLayer);
    bool leftRay = Physics.Raycast(rayStartLeft, Vector3.down, distance, movementSTats.GroundLayer);
    bool rightRay = Physics.Raycast(rayStartRight, Vector3.down, distance, movementSTats.GroundLayer);

    // Final grounded check: Combine BoxCast and Raycasts
    _isGrounded = _isGrounded || (centerRay || leftRay || rightRay);

    // Debug Visualizations
    if (movementSTats.DebugShowIsGroundedBox)
    {
        Debug.DrawRay(rayStartCenter, Vector3.down * distance, Color.blue);
        Debug.DrawRay(rayStartLeft, Vector3.down * distance, Color.blue);
        Debug.DrawRay(rayStartRight, Vector3.down * distance, Color.blue);
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
