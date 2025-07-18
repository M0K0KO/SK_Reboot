using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct PlayerTransformSyncSystem : ISystem
{
    private EntityQuery playerQuery;
    
    public void OnCreate(ref SystemState state)
    {
        playerQuery = state.GetEntityQuery(ComponentType.ReadOnly<PlayerTag>(), ComponentType.ReadOnly<LocalTransform>());

    }

    public void OnUpdate(ref SystemState state)
    {
        
        if (PlayerTransformManager.Instance == null)
        {
            return;
        }

        if (playerQuery.TryGetSingleton(out LocalTransform playerTransform))
        {
            PlayerTransformManager.Instance.transform.position = playerTransform.Position;
        }
    }
}