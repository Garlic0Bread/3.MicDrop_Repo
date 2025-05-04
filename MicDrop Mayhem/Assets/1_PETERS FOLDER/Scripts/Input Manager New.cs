// Updated InputManagerNew.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManagerNew : MonoBehaviour
{
    // Instance-based properties instead of static
    public PlayerInput PlayerInput { get; private set; }
    public Vector2 Movement { get; private set; }
    public bool JumpWasPressed { get; private set; }
    public bool JumpIsHeld { get; private set; }
    public bool JumpWasReleased { get; private set; }

    public bool AttackPressed { get; private set; }

    [SerializeField] private int _playerIndex = 0; // Serialized field for editor assignment

    // Public getter for player index (read-only)
    public int PlayerIndex => _playerIndex;

    public object Actions { get; internal set; }

    private InputAction _movementAction;
    private InputAction _jumpAction;
    private InputAction _dashAction;

    private InputAction _attackAction;

    private void Awake()
    {
        PlayerInput = GetComponent<PlayerInput>();

        // The player index is now determined by the PlayerInput component itself
        // We'll use our serialized _playerIndex for logical identification
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

    // Helper method to get input manager by logical player index
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