using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
public struct DeselectUnitsJob : IJobParallelFor
{
    [WriteOnly] 
    public NativeArray<bool> isSelected;

    public void Execute(int index)
    {
        isSelected[index] = false;
    }
}
