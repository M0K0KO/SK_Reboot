using System;
using Moko;
using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
    public BaseState currentState;

    public IdleState idleState;
    public MoveState moveState;

    public Animator animator;
    public PlayerInputManager inputManager;
    public CharacterController controller;
    public Camera playerCam;

    [Header("Static Properties")] 

    [Header("Player Properties")] 
    public float gravity = 9.81f;
    public float moveSpeed = 10f;
    public float rotationSpeed = 30f;
    [HideInInspector] public Vector3 playerDirection;

    private void Awake()
    {
        Initialize();
    }

    private void Update()
    {
        currentState.UpdateState();
    }


    private void Initialize()
    {
        inputManager= GetComponent<PlayerInputManager>();
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        playerCam = Camera.main;
        idleState = new IdleState(this);
        moveState = new MoveState(this);
        
        currentState = idleState;
        currentState.EnterState();
    }

    public void ChangeState(BaseState newState)
    {
        currentState.ExitState();
        currentState = newState;
        currentState.EnterState();
    }

    private Vector3 RotateInputVector(Vector2 rawInput)
    {
        Quaternion cameraFlatRotation = Quaternion.Euler(0f, playerCam.transform.eulerAngles.y, 0f);
        Vector3 moveInput3D = new Vector3(rawInput.x, 0f, rawInput.y);
        
        return cameraFlatRotation * moveInput3D;
    }
    
    public void UpdatePlayerDirection()
    {
        Vector3 newDirection = RotateInputVector(inputManager.moveInput);
        
        if (newDirection.sqrMagnitude > 0)
        {
            newDirection.Normalize();
        }

        playerDirection = newDirection;
    }

    public void ApplyGravity()
    {
        controller.Move(Vector3.down * gravity * Time.deltaTime);
    }
}
