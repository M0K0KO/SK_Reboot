using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct CreatePathRequestsJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<bool> isAlive;
    [ReadOnly] public NativeArray<bool> isSelected;
    [ReadOnly] public NativeArray<float3> positions;
    [ReadOnly] public NativeArray<float3> targetPositions;
    [ReadOnly] public PathfindingGrid grid; // GridBuilder.pathfindingGrid.grid
    
    [ReadOnly] public NativeArray<int> pathLength;
    [ReadOnly] public NativeArray<int> pathDataStartIndex;
    [ReadOnly] public NativeList<float3> allPathNodes;
    
    public NativeList<PathfindingRequestData>.ParallelWriter pathRequests;

    public void Execute(int index)
    {
        // 살아있고, 선택된 유닛만 처리
        if (!isAlive[index] || !isSelected[index])
        {
            return;
        }
        
        float3 currentDestination;
        if (pathLength[index] > 0)
        {
            // 현재 경로가 있다면, 경로의 마지막 노드가 최종 목적지
            int lastNodeIndex = pathDataStartIndex[index] + pathLength[index] - 1;
            currentDestination = allPathNodes[lastNodeIndex];
        }
        else
        {
            // 경로가 없다면, 현재 위치가 최종 목적지 (멈춰있음)
            currentDestination = positions[index];
        }
        
        // 현재 위치가 목표위치와 같은 경우 return
        int2 curNode = GetNodeIndex(positions[index]);
        int2 targetNode = GetNodeIndex(targetPositions[index]);
        if (math.all(curNode == targetNode)) return;

        // 목표 지점이 유효하지 않은 경우 return
        if (!grid.grid[targetNode.x + targetNode.y * grid.width].walkable) return;
        
        if (math.distancesq(currentDestination, targetPositions[index]) > 1f) 
        {
            pathRequests.AddNoResize(new PathfindingRequestData
            {
                unitIndex = index,
                startPos = positions[index],
                endPos = targetPositions[index]
            });
        }
    }
    
    public int2 GetNodeIndex(float3 worldPosition)
    {
        float3 localPos = worldPosition;

        int rx = (int)((localPos.x / grid.nodeSize) + (grid.width / 2));
        int ry = (int)((localPos.z / grid.nodeSize) + (grid.height / 2));

        if(rx < 0 || rx >= grid.width || ry < 0 || ry >= grid.height) {
            return new int2 { x = -1, y = -1 };
        }

        return new int2 { x = rx, y = ry };
    }
}
