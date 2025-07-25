using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct CalculateSeparationDirectionJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<UnitState> unitState;
    [ReadOnly] public NativeArray<float3> positions;
    [ReadOnly] public float separationRadius;
    [ReadOnly] public float separationWeight; 
    [ReadOnly] public NativeParallelMultiHashMap<int, int> spatialHashMap;
    [ReadOnly] public float cellSize;
    
    public NativeArray<float3> desiredDirections;

    private static int GetHash(float3 pos, float cellSize)
    {
        int x = (int)math.floor(pos.x / cellSize);
        int z = (int)math.floor(pos.z / cellSize);
        return x * 73856093 ^ z * 19349663;
    }
    
    
    public void Execute(int index)
    {
        if (unitState[index] == UnitState.Idle) return;
        
        int searchRange = (int)math.ceil(separationRadius / cellSize);
        
        float3 currentPos = positions[index];
        float3 separationVector = float3.zero;
        int neighborCount = 0;

        for (int i = -searchRange; i <= searchRange; i++)
        {
            for (int j = -searchRange; j <= searchRange; j++)
            {
                int cellHash = GetHash(currentPos + new float3(i * cellSize, 0, j * cellSize), cellSize);
                if (spatialHashMap.TryGetFirstValue(cellHash, out int neighborIndex, out var iterator))
                {
                    do
                    {
                        if (index == neighborIndex) continue;

                        float3 neighborPos = positions[neighborIndex];
                        if (math.distancesq(currentPos, neighborPos) < separationRadius * separationRadius)
                        {
                            separationVector += currentPos - neighborPos;
                            neighborCount++;
                        }
                    }
                    while (spatialHashMap.TryGetNextValue(out neighborIndex, ref iterator));
                }
            }
        }
        
        if (neighborCount > 0)
        {
            separationVector /= neighborCount;
            separationVector.y = 0;
            
            desiredDirections[index] += math.normalizesafe(separationVector) * separationWeight;
        }
    }
}
