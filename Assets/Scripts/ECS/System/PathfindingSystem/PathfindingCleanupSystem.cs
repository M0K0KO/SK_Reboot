using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

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

        foreach (var (pathFinder, 
                     pathFollower,
                     path, 
                     physicsVelocity,
                     entity) 
                 in SystemAPI.Query<
                         RefRW<PathFinder>,
                         RefRO<PathFollower>, 
                         DynamicBuffer<PathWaypoint>,
                         RefRW<PhysicsVelocity>>()
                     .WithEntityAccess())
        {
            if (pathFollower.ValueRO.WaypointIndex >= path.Length)
            {
                physicsVelocity.ValueRW.Linear = float3.zero;
                
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