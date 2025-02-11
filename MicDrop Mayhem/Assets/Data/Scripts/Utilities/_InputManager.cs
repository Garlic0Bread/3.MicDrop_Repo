using System.Collections;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine;

public class _InputManager : MonoBehaviour
{
    [HideInInspector] public Player1Controls p1Controls;
    public static _InputManager instance;
    public static PlayerInput playerInput;
    public static Vector2 P1_Movement;
    public static Vector2 P2_Movement;

    public static bool Q_SPbtn_P1;
    public static bool UP_SPbtn_P2;
    public static bool LEFT_SPattack;
    public static bool RIGHT_SPattack;
    private InputAction P1_SPbtnQ;
    private InputAction P2_SPbtnUP;
    private InputAction LEFTbtn_SPattack;
    private InputAction RIGHTbtn_SPattack;

    [Header("Player 1 Input Settings")]
    public static bool P1_runIsHeld;
    public static bool P1_jumpIsHeld;
    public static bool P1_jumpWasPressed;
    public static bool P1_jumpWasReleased;
    public static bool P1_isLightAttacking;
    private InputAction P1_attackingAction;
    private InputAction P1_moveAction;
    private InputAction P1_jumpAction;
    private InputAction P1_runAction;

    [Header("Player 2 Input Settings")]
    public static bool P2_jumpIsHeld;
    public static bool P2_jumpWasPressed;
    public static bool P2_jumpWasReleased;
    public static bool P2_isLightAttacking;
    private InputAction P2_attackingAction;
    private InputAction P2_moveAction;
    private InputAction P2_jumpAction;


    private void OnEnable()
    {
        p1Controls.Enable();
    }
    private void OnDisable()
    {
        p1Controls?.Disable();
    }

    private void Awake()
    {
        p1Controls = new Player1Controls();
        playerInput = GetComponent<PlayerInput>();

        P1_attackingAction = playerInput.actions["Attack"];
        P1_moveAction = playerInput.actions["P1_Movement"];
        P1_jumpAction = playerInput.actions["Jump"];
        P1_runAction = playerInput.actions["Dash"];

        P2_attackingAction = playerInput.actions["Attack_P2"];
        P2_moveAction = playerInput.actions["P2_Movement"];
        P2_jumpAction = playerInput.actions["Jump_P2"];


        P1_SPbtnQ = playerInput.actions["P1_SPbtnQ"];

        P2_SPbtnUP = playerInput.actions["P2_SPbtnUP"];
        LEFTbtn_SPattack = playerInput.actions["SPbtnLEFT"];
        RIGHTbtn_SPattack = playerInput.actions["SPbtnRIGHT"];
    }

    private void Update()
    {
        P1_Movement = P1_moveAction.ReadValue<Vector2>();
        P1_runIsHeld = P1_runAction.IsPressed();
        P1_jumpIsHeld = P1_jumpAction.IsPressed();
        P1_jumpWasPressed = P1_jumpAction.WasPressedThisFrame();
        P1_isLightAttacking = P1_attackingAction.WasPressedThisFrame();
        P1_jumpWasReleased = P1_jumpAction.WasReleasedThisFrame();

        P2_jumpIsHeld = P2_jumpAction.IsPressed();
        P2_Movement = P2_moveAction.ReadValue<Vector2>();
        P2_jumpWasPressed = P2_jumpAction.WasPressedThisFrame();
        P2_jumpWasReleased = P2_jumpAction.WasReleasedThisFrame();
        P2_isLightAttacking = P2_attackingAction.WasPressedThisFrame();


        Q_SPbtn_P1 = P1_SPbtnQ.WasPressedThisFrame();

        UP_SPbtn_P2 = P2_SPbtnUP.WasPressedThisFrame();
       
    }
}