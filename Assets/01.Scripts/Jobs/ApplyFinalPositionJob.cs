using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct ApplyFinalPositionJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> pathPositions;
    [ReadOnly] public NativeArray<float3> separationOffsets;
    
    public NativeArray<float3> finalPositions;

    public void Execute(int index)
    {
        finalPositions[index] = pathPositions[index] + separationOffsets[index];
    }
}
