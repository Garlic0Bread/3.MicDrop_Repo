using System.Collections;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine;

public class _InputManager_P2 : MonoBehaviour
{
   [HideInInspector] public Player2_Controls p2Controls;
    public static _InputManager_P2 instance;
    public static PlayerInput playerInput;
    public static Vector2 Movement;

    public static bool runIsHeld;
    public static bool isLightAttacking;
    public static bool jumpIsHeld;
    public static bool jumpWasPressed;
    public static bool jumpWasReleased;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction runAction;
    private InputAction attackingAction;

    private void OnEnable()
    {
        p2Controls.Enable();
    }
    private void OnDisable()
    {
        p2Controls?.Disable();
    }

    private void Awake()
    {
        p2Controls = new Player2_Controls();
        playerInput = GetComponent<PlayerInput>();

        attackingAction = playerInput.actions["Attack_P2"];
        moveAction = playerInput.actions["P2_Movement"];
        jumpAction = playerInput.actions["Jump_P2"];
        runAction = playerInput.actions["Dash_P2"];
    }

    private void Update()
    {
        Movement = moveAction.ReadValue<Vector2>();
        runIsHeld = runAction.IsPressed();
        jumpIsHeld = jumpAction.IsPressed();
        jumpWasPressed = jumpAction.WasPressedThisFrame();
        isLightAttacking = attackingAction.WasPressedThisFrame();
        jumpWasReleased = jumpAction.WasReleasedThisFrame();
    }
}
