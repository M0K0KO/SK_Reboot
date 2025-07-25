using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
public struct MemCopyJob<T> : IJob where T : struct
{
    [ReadOnly] public NativeArray<T> Source;
    [WriteOnly] public NativeArray<T> Destination;

    public void Execute()
    {
        Destination.CopyFrom(Source);
    }
}
