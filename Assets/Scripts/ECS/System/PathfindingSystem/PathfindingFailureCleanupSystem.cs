using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))] 
[UpdateAfter(typeof(PathfindingCompletionSystem))]
partial struct PathfindingFailureCleanupSystem : ISystem
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

        foreach (var (pathFinder, localTransform) in
                 SystemAPI.Query<RefRW<PathFinder>, RefRO<LocalTransform>>())
        {
            if (pathFinder.ValueRO.status == PathStatus.Failed)
            {
                pathFinder.ValueRW.currentTargetPosition = localTransform.ValueRO.Position;
                pathFinder.ValueRW.status = PathStatus.Idle;
            }
        }
    }
}