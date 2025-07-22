using UnityEngine;

public class MoveState : BaseState
{
    public MoveState(PlayerStateMachine machine) : base(machine) {}

    public override void EnterState()
    {
        machine.animator.CrossFadeInFixedTime("PlayerMove", 0.2f);
    }

    public override void UpdateState()
    {
        machine.ApplyGravity();
        
        machine.UpdatePlayerDirection();
        MovePlayer();
        RotatePlayer();
        
        if (machine.inputManager.moveInput == Vector2.zero)
        {
            machine.ChangeState(machine.idleState);
        }
    }

    public override void ExitState()
    {
        
    }
    
    private void MovePlayer()
    {
        machine.controller.Move(machine.playerDirection * (machine.moveSpeed * Time.deltaTime));
    }

    private void RotatePlayer()
    {
        if (machine.playerDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(machine.playerDirection);
            
            machine.transform.rotation = Quaternion.Slerp(
                machine.transform.rotation,
                targetRotation,
                machine.rotationSpeed * Time.deltaTime
            );
        }
    }
    
}
