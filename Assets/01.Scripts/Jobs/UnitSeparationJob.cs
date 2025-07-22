using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct UnitSeparationJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> positions;
    [ReadOnly] public NativeArray<bool> isAlive;
    [ReadOnly] public float separationRadiusSq;
    [ReadOnly] public float separationForce;
    [ReadOnly] public float deltaTime;
    [ReadOnly] public NativeParallelMultiHashMap<int, int> spatialHashMap;
    [ReadOnly] public float cellSize;
    [ReadOnly] public NativeArray<int> pathLength;

    [WriteOnly] public NativeArray<float3> separationOffsets;

    private static int GetHash(float3 pos, float cellSize)
    {
        int x = (int)math.floor(pos.x / cellSize);
        int z = (int)math.floor(pos.z / cellSize);
        return x * 73856093 ^ z * 19349663;
    }
    
    
    public void Execute(int index)
    {
        separationOffsets[index] = float3.zero;
        
        if (!isAlive[index]) return;

        if (!isAlive[index] || pathLength[index] == 0)
        {
            return;
        }

        float3 currentPos = positions[index];
        float3 separationVector = float3.zero;
        int neighborCount = 0;

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                int cellHash = GetHash(currentPos + new float3(i * cellSize, 0, j * cellSize), cellSize);

                if (spatialHashMap.TryGetFirstValue(cellHash, out int neighborIndex, out var iterator))
                {
                    do
                    {
                        if (index == neighborIndex) continue;

                        float3 neighborPos = positions[neighborIndex];
                        if (math.distancesq(currentPos, neighborPos) < separationRadiusSq)
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
            // 평균 분리 벡터를 계산하고 적용
            separationVector /= neighborCount;
            separationVector.y = 0;
            separationOffsets[index] = math.normalize(separationVector) * separationForce * deltaTime;
        }
    }
}
