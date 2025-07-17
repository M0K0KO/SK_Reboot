using Unity.Burst;
using Unity.Entities;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(PathfindingFailureSystem))]
partial struct PathfindingCleanupSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (pathFinder, pathFollower, path, entity) in
                 SystemAPI.Query<RefRW<PathFinder>, RefRO<PathFollower>, DynamicBuffer<PathWaypoint>>()
                     .WithEntityAccess())
        {
            if (pathFollower.ValueRO.WaypointIndex >= path.Length)
            {
                ecb.RemoveComponent<PathFollower>(entity);
                ecb.RemoveComponent<PathWaypoint>(entity);

                if (SystemAPI.HasComponent<PathFinder>(entity))
                {
                    pathFinder.ValueRW.status = PathStatus.Idle;
                }
            }
        }
    }
}