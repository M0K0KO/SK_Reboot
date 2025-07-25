using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct ChangeStateToMoveJob : IJob
{
    [ReadOnly] public NativeArray<int> indicesToChange;

    public NativeArray<int> unitAnimationID;
    public NativeArray<UnitState> unitState;

    public void Execute()
    {
        for (int i = 0; i < indicesToChange.Length; i++)
        {
            int unitIndex = indicesToChange[i];
            unitState[unitIndex] = UnitState.Move;
            unitAnimationID[unitIndex] = 1;
        }
    }
}
