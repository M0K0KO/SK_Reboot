using System.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct UnitMoveJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<UnitState> unitState;
    [ReadOnly] public NativeArray<float3> positions;
    public NativeArray<float3> nextPositions;
    public NativeArray<quaternion> rotations;

    public NativeArray<int> currentPathNodeIndex;
    public NativeArray<int> pathLength; // path 끝나면 0으로

    public NativeList<UnitStateChangeCommand>.ParallelWriter stateChangeCommandBuffer;

    [ReadOnly] public NativeArray<bool> isAlive;
    [ReadOnly] public NativeArray<int> pathDataStartIndex;
    [ReadOnly] public NativeList<float3> allPathNodes;

    [ReadOnly] public float deltaTime;
    [ReadOnly] public float moveSpeed;
    [ReadOnly] public float rotationSpeed;
    [ReadOnly] public float waypointReachDistanceSq; 

    public void Execute(int index)
    {
        if (unitState[index] != UnitState.Move) return;
        
        nextPositions[index] = positions[index];
        
        if (!isAlive[index] || pathLength[index] == 0) return;

        int currentIdx = currentPathNodeIndex[index];

        //경로 끝났나??
        if (currentIdx >= pathLength[index])
        {
            pathLength[index] = 0; // 이동 종료!!
            return;
        }
        
        int pathStart = pathDataStartIndex[index];
        float3 targetWaypoint = allPathNodes[pathStart + currentIdx];
        float3 currentPosition = positions[index];
        
        // wayPoint 도착했나??
        if (math.distancesq(currentPosition, targetWaypoint) < waypointReachDistanceSq)
        {
            currentIdx++;
            currentPathNodeIndex[index] = currentIdx;

            if (currentIdx >= pathLength[index])
            {
                stateChangeCommandBuffer.AddNoResize(new UnitStateChangeCommand()
                {
                    index = index,
                    newState = UnitState.Idle,
                });
                
                pathLength[index] = 0; // 이동 종료!!
                return;
            }
            targetWaypoint = allPathNodes[pathStart + currentIdx];
        }

        float3 moveDirection = targetWaypoint - currentPosition;
        
        if (math.lengthsq(moveDirection) > 0.0001f)
        {
            moveDirection.y = 0;

            moveDirection = math.normalize(moveDirection);

            nextPositions[index] = currentPosition + moveDirection * moveSpeed * deltaTime;

            quaternion targetRotation = quaternion.LookRotation(moveDirection, math.up());
            rotations[index] = math.slerp(rotations[index], targetRotation, rotationSpeed * deltaTime);
        }
    }
}
