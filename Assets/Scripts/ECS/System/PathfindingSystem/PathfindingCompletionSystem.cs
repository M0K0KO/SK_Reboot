using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;


[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))] 
[UpdateAfter(typeof(ProcessPathfindingRequestSystem))]
partial struct PathfindingCompletionSystem : ISystem
{
    private PathfindingGrid _pathfindingGrid;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PathfindingGrid>();
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _pathfindingGrid = SystemAPI.GetSingleton<PathfindingGrid>();
        
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        foreach (var (pathFinder, entity) in SystemAPI.Query<RefRW<PathFinder>>().WithEntityAccess())
        {
            // Path Job이 끝났는지 진행중인지 확인
            if (pathFinder.ValueRO.status != PathStatus.InProgress) continue;
            if (!pathFinder.ValueRO.jobHandle.IsCompleted) continue;
            
            pathFinder.ValueRW.jobHandle.Complete();
            
            // 결과를 반환해주기 위해 하나 생성
            var pathResult = pathFinder.ValueRO.pathBuffer;
            if (pathResult.Length > 0)
            {
                var pathBuffer = ecb.AddBuffer<PathWaypoint>(entity);
                for (int i = pathResult.Length - 1; i >= 0; i--)
                {
                    float3 worldPos = _pathfindingGrid.GetNodePosition(pathResult[i].x, pathResult[i].y);
                    pathBuffer.Add(new PathWaypoint { position = worldPos });
                }

                pathFinder.ValueRW.status = PathStatus.Ready;
                
                // Path를 찾았고, 이동만 하면 됨 -> PathFollower를 넣어줌
                ecb.AddComponent(entity, new PathFollower
                {
                    WaypointIndex = 0,
                });
            }
            else
            {
                pathFinder.ValueRW.status = PathStatus.Failed;
            }
            
        }
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
