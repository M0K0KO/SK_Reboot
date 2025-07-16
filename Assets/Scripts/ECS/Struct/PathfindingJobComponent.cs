using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

public struct PathfindingJobComponent : IComponentData, IDisposable
{
    public JobHandle jobHandle;
    public NativeList<int2> pathResult;
    
    public void Dispose()
    {
        if (pathResult.IsCreated)
        {
            pathResult.Dispose();
        }
    }
}
