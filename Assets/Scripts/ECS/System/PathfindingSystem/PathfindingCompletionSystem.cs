using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;


[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))] 
[UpdateAfter(typeof(PathfindingRequestSystem))]
partial struct PathfindingCompletionSystem : ISystem
{
    private PathfindingGrid pathfindingGrid;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PathfindingGrid>();
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        foreach (var (request, jobComponent, entity) in
                 SystemAPI.Query<RefRW<PathRequest>, RefRW<PathfindingJobComponent>>().WithEntityAccess())
        {
            if (!jobComponent.ValueRO.jobHandle.IsCompleted) continue;
            
            jobComponent.ValueRW.jobHandle.Complete();
            
            var pathResult = jobComponent.ValueRO.pathResult;
            if (pathResult.Length > 0)
            {
                var pathBuffer = ecb.AddBuffer<PathWaypoint>(entity);
                for (int i = pathResult.Length - 1; i >= 0; i--)
                {
                    float3 worldPos = pathfindingGrid.GetNodePosition(pathResult[i].x, pathResult[i].y);
                    pathBuffer.Add(new PathWaypoint { position = worldPos });
                }
                
                ecb.RemoveComponent<PathRequest>(entity);
            }
            else
            {
                request.ValueRW.requestStatus = RequestStatus.Failed;
            }
            
            jobComponent.ValueRW.Dispose();
            ecb.RemoveComponent<PathfindingJobComponent>(entity);
            ecb.RemoveComponent<PathRequest>(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
