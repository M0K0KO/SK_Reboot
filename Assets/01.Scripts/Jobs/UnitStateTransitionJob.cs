using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
public struct UnitStateTransitionJob : IJob
{
    public NativeArray<int> unitAnimationID;
    public NativeArray<UnitState> unitState;

    [ReadOnly] public NativeList<UnitStateChangeCommand> unitStateChangeCommandBuffer;
    

    public void Execute()
    {
        for (int i = 0; i < unitStateChangeCommandBuffer.Length; i++)
        {
            var command = unitStateChangeCommandBuffer[i];
            if (unitState[command.index] != command.newState)
            {
                unitState[command.index] = command.newState;
                unitAnimationID[command.index] = GetStateAnimationID(command.newState);
            }
        }
    }

    public int GetStateAnimationID(UnitState unitState)
    {
        switch(unitState)
        {
            case UnitState.Idle:
                return 0;
            case UnitState.Move:
                return 1;
        }
        return 0;
    }
}
