using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct UpdatePathStateJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<UnitState> unitState;
    [ReadOnly] public NativeArray<float3> positions;
    [ReadOnly] public NativeArray<bool> isAlive;
    [ReadOnly] public NativeArray<int> pathDataStartIndex;
    [ReadOnly] public NativeList<float3> allPathNodes;
    [ReadOnly] public float waypointReachDistanceSq;

    public NativeArray<int> currentPathNodeIndex;
    public NativeArray<int> pathLength;

    public NativeList<UnitStateChangeCommand>.ParallelWriter stateChangeCommandBuffer;

    public void Execute(int index)
    {
        if (unitState[index] != UnitState.Move || !isAlive[index] || pathLength[index] == 0)
        {
            return;
        }

        int currentIdx = currentPathNodeIndex[index];

        if (currentIdx >= pathLength[index])
        {
            return;
        }
        
        int pathStart = pathDataStartIndex[index];
        float3 currentPosition = positions[index];
        float3 targetWaypoint = allPathNodes[pathStart + currentIdx];
        
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
                
                pathLength[index] = 0; 
            }
        }
    }
}