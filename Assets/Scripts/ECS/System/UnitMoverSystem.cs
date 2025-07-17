using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
partial struct UnitMoverSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (localTransform, 
                     physicsVelocity, 
                     path, 
                     pathFollower,
                     unitMover) 
                 in SystemAPI.Query
                    <RefRW<LocalTransform>, 
                     RefRW<PhysicsVelocity>, 
                     DynamicBuffer<PathWaypoint>,
                     RefRW<PathFollower>,
                     RefRO<UnitMover>>())
        {
            if (pathFollower.ValueRO.WaypointIndex >= path.Length) continue;

            float3 targetPosition = path[pathFollower.ValueRO.WaypointIndex].position;
            float distanceSq = math.distancesq(localTransform.ValueRO.Position, targetPosition);

            if (distanceSq < 4f) // 현재 목표인 waypoint까지 도달함
            {
                pathFollower.ValueRW.WaypointIndex++;
            }
            else // 아직 현재 목표까지 도달하지 못함
            {
                float3 direction = targetPosition - localTransform.ValueRO.Position;
                direction.y = 0;
                direction = math.normalize(direction);

                physicsVelocity.ValueRW.Linear = direction * unitMover.ValueRO.moveSpeed;
                physicsVelocity.ValueRW.Angular = float3.zero;
                localTransform.ValueRW.Rotation = 
                    math.slerp(localTransform.ValueRO.Rotation, 
                        quaternion.LookRotation(direction, math.up()), 
                        SystemAPI.Time.DeltaTime * unitMover.ValueRO.rotationSpeed);
            }
        }
    }
}