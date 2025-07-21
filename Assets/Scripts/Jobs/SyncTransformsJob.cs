using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;

[BurstCompile]
public struct SyncTransformsJob : IJobParallelForTransform
{
    [ReadOnly] public NativeArray<float3> positions;
    [ReadOnly] public NativeArray<quaternion> rotations;
    [ReadOnly] public NativeArray<bool> isAlive;

    public void Execute(int index, TransformAccess transform)
    {
        if (isAlive[index])
        {
            transform.position = positions[index];
            transform.rotation = rotations[index];
        }
    }
}