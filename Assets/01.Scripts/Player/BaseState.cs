using UnityEngine;

public class BaseState
{
    protected PlayerStateMachine machine;

    public BaseState(PlayerStateMachine machine)
    {
        this.machine = machine;
    }
    
    public virtual void EnterState() {}
    public virtual void UpdateState() {}
    public virtual void ExitState() {}
}
