using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))] 
partial struct PathFinderManageSystem : ISystem
{
    private PathfindingGrid _pathfindingGrid;
    
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PathfindingGrid>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if(SystemAPI.TryGetSingleton<PathfindingGrid>(out _pathfindingGrid)) {

            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (localTransform, pathFinder, unitMover) in SystemAPI.Query
                         <RefRO<LocalTransform>, RefRW<PathFinder>, RefRO<UnitMover>>())
            {
                // 클릭해서 들어온 targetPos가 walkable한 node인지 확인
                int2 targetNodeIndex = _pathfindingGrid.GetNodeIndex(pathFinder.ValueRO.currentTargetPosition);
                if (targetNodeIndex.x < 0 || targetNodeIndex.y < 0) continue;
                Node targetNode = _pathfindingGrid.GetNode(targetNodeIndex.x, targetNodeIndex.y);

                if (!targetNode.walkable) continue;

                // 이후 로직...
                bool isArrived =
                    math.distancesq(pathFinder.ValueRO.currentTargetPosition, localTransform.ValueRO.Position) < 5f;
                float currentTargetToNewTargetDistancesq = math.distancesq(pathFinder.ValueRO.currentTargetPosition,
                    unitMover.ValueRO.targetPosition);

                if (currentTargetToNewTargetDistancesq > 5f) // 원래 목적지랑 다른 곳을 찍으면?
                {
                    pathFinder.ValueRW.currentTargetPosition = unitMover.ValueRO.targetPosition;
                    pathFinder.ValueRW.status = PathStatus.Requested;
                }
                else
                {
                    if (isArrived)
                    {
                        pathFinder.ValueRW.pathBuffer.Clear();
                        pathFinder.ValueRW.status = PathStatus.Idle;
                    }
                }
            }
        }
    }
}
