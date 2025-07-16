using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))] 
[UpdateAfter(typeof(PathFinderManageSystem))]
partial struct ProcessPathfindingRequestSystem : ISystem
{
    private PathfindingGrid _pathfindingGrid;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<PathfindingGrid>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _pathfindingGrid = SystemAPI.GetSingleton<PathfindingGrid>();

        // PathRequest (without PathfindingJobComponent)
        // -> PathRequest를 갖고 있지만 아직 Request를 처리하지 않은 것들을 모아 Request를 처리해줌
        foreach (var (pathFinder, localTransform) in
                 SystemAPI.Query<RefRW<PathFinder>, RefRO<LocalTransform>>())
        {
            if (pathFinder.ValueRO.status != PathStatus.Requested) // request상태가 requested가 아니라면 continue;
            {
                continue;
            }

            pathFinder.ValueRW.status = PathStatus.InProgress;
            pathFinder.ValueRW.pathBuffer.Clear();
            
            // ProcessPathJob을 생성
            var processJob = new ProcessPathJob
            {
                srcPosition = localTransform.ValueRO.Position,
                dstPosition = pathFinder.ValueRO.currentTargetPosition,
                grid = _pathfindingGrid,
                result = pathFinder.ValueRW.pathBuffer,
            };
            
            JobHandle jobHandle = processJob.Schedule(state.Dependency);
            pathFinder.ValueRW.jobHandle = jobHandle;
            state.Dependency = jobHandle; 
        }
    }
}