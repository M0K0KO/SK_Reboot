using System.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct CalculatePathDirectionJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<UnitState> unitState;
    [ReadOnly] public NativeArray<float3> positions;
    [ReadOnly] public NativeArray<int> currentPathNodeIndex;
    [ReadOnly] public NativeArray<int> pathLength; 
    [ReadOnly] public NativeArray<bool> isAlive;
    [ReadOnly] public NativeArray<int> pathDataStartIndex;
    [ReadOnly] public NativeList<float3> allPathNodes;
    
    [WriteOnly] public NativeArray<float3> desiredDirections;
    
    public void Execute(int index)
    {
        if (unitState[index] != UnitState.Move || !isAlive[index] || pathLength[index] == 0)
        {
            desiredDirections[index] = float3.zero;
            return;
        }

        // 경로가 끝났으면 방향을 0으로 설정
        int currentIdx = currentPathNodeIndex[index];
        if (currentIdx >= pathLength[index])
        {
            desiredDirections[index] = float3.zero;
            return;
        }
        
        int pathStart = pathDataStartIndex[index];
        float3 currentPosition = positions[index];
        float3 targetWaypoint = allPathNodes[pathStart + currentIdx];

        // 현재 유닛이 따라가야 할 경로 '선분' 정의
        float3 previousWaypoint = (currentIdx > 0) 
            ? allPathNodes[pathStart + currentIdx - 1] 
            : currentPosition;

        float3 segmentVector = targetWaypoint - previousWaypoint;
        float3 desiredDirection;

        if (math.lengthsq(segmentVector) < 0.001f)
        {
            desiredDirection = targetWaypoint - currentPosition;
        }
        else
        {
            float3 segmentDir = math.normalize(segmentVector);
            float3 toUnitVector = currentPosition - previousWaypoint;
            float projectionDistance = math.dot(toUnitVector, segmentDir);
            float3 pointOnPath = previousWaypoint + segmentDir * math.clamp(projectionDistance, 0, math.length(segmentVector));

            float lookAheadDistance = 1.0f; 
            float3 seekPosition = pointOnPath + segmentDir * lookAheadDistance;

            desiredDirection = seekPosition - currentPosition;
        }

        // 계산된 방향을 정규화하여 버퍼에 씁니다.
        desiredDirections[index] = math.normalizesafe(desiredDirection);
    }
}
