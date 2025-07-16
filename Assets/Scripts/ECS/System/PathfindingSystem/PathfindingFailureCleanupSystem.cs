using Unity.Burst;
using Unity.Entities;

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
        
        foreach (var (request, entity) in SystemAPI.Query<RefRO<PathRequest>>().WithEntityAccess())
        {
            if (request.ValueRO.requestStatus == RequestStatus.Failed)
            {
                ecb.RemoveComponent<PathRequest>(entity);
            }
        }
    }
}