// InputManagerNew.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManagerNew : MonoBehaviour
{
    public static PlayerInput PlayerInput;
    public static Vector2 Movement;
    public static bool JumpWasPressed;
    public static bool JumpIsHeld;
    public static bool JumpWasReleased;
    
    // REMOVED: public static bool RunWasPressedThisFrame;
    
    private InputAction _movementAction;
    private InputAction _jumpAction;

    private void Awake()
    {
        PlayerInput = GetComponent<PlayerInput>();
        _movementAction = PlayerInput.actions["Move"];
        _jumpAction = PlayerInput.actions["Jump"];
    }

    private void Update()
    {
        Movement = _movementAction.ReadValue<Vector2>();
        JumpWasPressed = _jumpAction.WasPressedThisFrame();
        JumpIsHeld = _jumpAction.IsPressed();
        JumpWasReleased = _jumpAction.WasReleasedThisFrame();
    }
}