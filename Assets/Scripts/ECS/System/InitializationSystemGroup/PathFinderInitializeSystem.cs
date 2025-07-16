using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct PathFinderInitializationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        bool didAddComponent = false;

        foreach (var (playerTag, entity) in SystemAPI.Query<PlayerTag>().WithNone<PathFinder>().WithEntityAccess())
        {
            ecb.AddComponent(entity, new PathFinder
            {
                status = PathStatus.Idle,
                pathBuffer = new NativeList<int2>(128, Allocator.Persistent),
                currentTargetPosition = float3.zero,
                jobHandle = default
            });

            didAddComponent = true;
        }

        if (didAddComponent)
        {
            ecb.Playback(state.EntityManager);
            
            state.Enabled = false;
        }
        ecb.Dispose();
    }
}