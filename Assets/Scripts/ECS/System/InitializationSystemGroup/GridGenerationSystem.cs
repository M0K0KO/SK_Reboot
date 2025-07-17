using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
[UpdateInGroup(typeof(InitializationSystemGroup))]
partial struct GridGenerationSystem : ISystem
{
    public const int obstacleProximityPenalty = 10;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PathfindingGridSingleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;
        
        var singletonEntity = SystemAPI.GetSingletonEntity<PathfindingGridSingleton>();
        var gridConfig = SystemAPI.GetSingletonRW<PathfindingGridSingleton>().ValueRO.gridConfig;
        var penaltyBuffer = SystemAPI.GetBuffer<MovementPenaltyElement>(singletonEntity);
        var nodeArray = new NativeArray<Node>(gridConfig.width * gridConfig.height, Allocator.Persistent);

        for (int x = 0; x < gridConfig.width; x++)
        {
            for (int y = 0; y < gridConfig.height; y++)
            {
                float3 worldPointXZ = new float3(
                    gridConfig.worldOffset.x - (gridConfig.nodeSize * gridConfig.width / 2) + (x * gridConfig.nodeSize) + (gridConfig.nodeSize / 2),
                    0,
                    gridConfig.worldOffset.z - (gridConfig.nodeSize * gridConfig.height / 2) + (y * gridConfig.nodeSize) + (gridConfig.nodeSize / 2)
                );

                var rayOrigin = new Vector3(worldPointXZ.x, gridConfig.gridBuildHeight, worldPointXZ.z);
                
                bool isWalkable = false;
                float yPosition = 0;
                int movementPenalty = 0;

                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, gridConfig.gridBuildHeight * 2, gridConfig.walkableLayers))
                {
                    yPosition = hit.point.y;
                    
                    foreach (var penaltyElement in penaltyBuffer)
                    {
                        if ((1 << hit.collider.gameObject.layer & penaltyElement.LayerMaskValue) != 0)
                        {
                            movementPenalty = penaltyElement.Penalty;
                            break; // 첫 번째로 일치하는 가중치를 적용
                        }
                    }

                    bool isObstructed = Physics.CheckBox(
                        hit.point + (Vector3.up * gridConfig.nodeRadius), 
                        Vector3.one * gridConfig.nodeRadius, 
                        Quaternion.identity, 
                        ~gridConfig.walkableLayers,
                        QueryTriggerInteraction.Ignore);
                    
                    isWalkable = !isObstructed;
                    if (isObstructed) movementPenalty += obstacleProximityPenalty;
                }
                
                int index = y * gridConfig.width + x;
                nodeArray[index] = new Node
                {
                    walkable = isWalkable,
                    yPosition = yPosition,
                    movementPenalty = movementPenalty,
                };
            }
        }

        int blurSize = 3;
        GridWeightBlurUtility.BlurPenaltyMap(ref nodeArray, gridConfig.width, gridConfig.height, blurSize);

        state.EntityManager.AddComponentData(singletonEntity, new PathfindingGrid
        {
            worldOffset = gridConfig.worldOffset,
            width = gridConfig.width,
            height = gridConfig.height,
            nodeSize = gridConfig.nodeSize,
            nodeRadius = gridConfig.nodeRadius,
            gridBuildHeight = gridConfig.gridBuildHeight,
            walkableLayers = gridConfig.walkableLayers,
            grid = nodeArray,
        });
    }
}
