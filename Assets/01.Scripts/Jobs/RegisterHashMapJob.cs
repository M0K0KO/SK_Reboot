using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct RegisterHashMapJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<bool> isAlive;
    [ReadOnly] public NativeArray<float3> positions;
    [ReadOnly] public float cellSize;

    public NativeParallelMultiHashMap<int, int>.ParallelWriter hashMap;

    private static int GetHash(float3 pos, float cellSize)
    {
        int x = (int)math.floor(pos.x / cellSize);
        int z = (int)math.floor(pos.z / cellSize);
        return x * 73856093 ^ z * 19349663;
    }
    
    public void Execute(int index)
    {
        if (!isAlive[index]) return;

        float3 pos = positions[index];
        int hash = GetHash(pos, cellSize);
        
        hashMap.Add(hash, index);
    }
}
