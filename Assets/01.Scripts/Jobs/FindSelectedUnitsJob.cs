using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
public struct FindSelectedUnitsJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<bool> isSelected;

    public NativeList<int>.ParallelWriter selectedUnitIndices;

    public void Execute(int index)
    {
        if (isSelected[index]) selectedUnitIndices.AddNoResize(index);
    }
}
