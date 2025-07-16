using Unity.Burst;
using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))] 
partial struct PathfindingRequestSystem : ISystem
{
    private PathfindingGrid pathfindingGrid;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<PathfindingGrid>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        var schedulingJob = new PathfindingSchedulingJob
        {
            pathfindingGrid = this.pathfindingGrid,
            ecb = ecb.AsParallelWriter()
        };

        state.Dependency = schedulingJob.ScheduleParallel(state.Dependency);
    }
}