using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct UpdateTargetPositionsJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<bool> isSelected;
    [WriteOnly] public NativeArray<float3> targetPosition;
    public float3 newPos;
    
    public void Execute(int index)
    {
        if (isSelected[index])
        {
            targetPosition[index] = newPos;
        }
    }
}
