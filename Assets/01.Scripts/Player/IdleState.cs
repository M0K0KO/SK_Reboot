using UnityEngine;

public class IdleState : BaseState
{
    public IdleState(PlayerStateMachine machine) : base(machine) {}

    public override void EnterState()
    {
        machine.animator.CrossFadeInFixedTime("PlayerIdle", 0.2f);
    }

    public override void UpdateState()
    {
        machine.ApplyGravity();
        
        if (machine.inputManager.moveInput != Vector2.zero)
        {
            machine.ChangeState(machine.moveState);
        }
    }

    public override void ExitState()
    {
        
    }
}
