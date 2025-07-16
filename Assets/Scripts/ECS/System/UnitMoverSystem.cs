using Unity.Burst;
using Unity.Entities;

partial struct UnitMoverSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
    }
}