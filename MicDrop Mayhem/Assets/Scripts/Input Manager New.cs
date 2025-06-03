using UnityEngine;
using UnityEngine.InputSystem;

public class InputManagerNew : MonoBehaviour
{
    // Instance-based properties
    public PlayerInput PlayerInput { get; private set; }
    public Vector2 Movement { get; private set; }
    public bool JumpWasPressed { get; private set; }
    public bool JumpIsHeld { get; private set; }
    public bool JumpWasReleased { get; private set; }
    public bool AttackPressed { get; private set; }
    public float VerticalInput => Movement.y; // New: Expose vertical input

    [SerializeField] private int _playerIndex = 0;
    public int PlayerIndex => _playerIndex;

    private InputAction _movementAction;
    private InputAction _jumpAction;
    private InputAction _dashAction;
    private InputAction _attackAction;

    private void Awake()
    {
        PlayerInput = GetComponent<PlayerInput>();
        _movementAction = PlayerInput.actions["Move"];
        _jumpAction = PlayerInput.actions["Jump"];
        _dashAction = PlayerInput.actions["Dash"];
        _attackAction = PlayerInput.actions["Attack"];
    }

    private void Update()
    {
        Movement = _movementAction.ReadValue<Vector2>();
        JumpWasPressed = _jumpAction.WasPressedThisFrame();
        JumpIsHeld = _jumpAction.IsPressed();
        JumpWasReleased = _jumpAction.WasReleasedThisFrame();
        AttackPressed = _attackAction.WasPressedThisFrame();
    }

    [System.Obsolete]
    public static InputManagerNew GetByPlayerIndex(int index)
    {
        var allManagers = FindObjectsOfType<InputManagerNew>();
        foreach (var manager in allManagers)
        {
            if (manager._playerIndex == index)
                return manager;
        }
        return null;
    }
}