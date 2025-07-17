using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;


[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(PathfindingCompletionSystem))]
public partial struct PathfindingFailureSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (pathFinder,
                     physicsVelocity,
                     localTransform,
                     entity)
                 in SystemAPI.Query<
                         RefRW<PathFinder>,
                         RefRW<PhysicsVelocity>,
                         RefRO<LocalTransform>>()
                     .WithEntityAccess())
        {
            if (pathFinder.ValueRO.status == PathStatus.Failed)
            {
                // Failed라면 타겟포지션을 자기 위치로 리셋하고, Idle로 바꿔주고 정지함.
                pathFinder.ValueRW.currentTargetPosition = localTransform.ValueRO.Position;
                physicsVelocity.ValueRW.Linear = float3.zero;
                pathFinder.ValueRW.status = PathStatus.Idle;

                if (SystemAPI.HasBuffer<PathWaypoint>(entity))
                {
                    ecb.SetBuffer<PathWaypoint>(entity).Clear();
                }
            }
        }
    }
}