using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class LayerPenalty
{
    public LayerMask layerMask;
    public int penalty;
}

public class PathfindingGridSingletonAuthoring : MonoBehaviour
{
    [Header("Grid Dimensions")] 
    public Vector2Int gridSize;
    public float gridBuildHeight = 100f;
    public float nodeRadius = 0.5f;

    public LayerMask walkableLayers;

    public LayerPenalty[] movementPenaltyLayers;
    
    
    public class Baker : Baker<PathfindingGridSingletonAuthoring>
    {
        public override void Bake(PathfindingGridSingletonAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            
            float nodeDiameter = authoring.nodeRadius * 2f;
            int width = Mathf.CeilToInt(authoring.gridSize.x / nodeDiameter);
            int height = Mathf.CeilToInt(authoring.gridSize.y / nodeDiameter);

            var penaltyBuffer = AddBuffer<MovementPenaltyElement>(entity);
            foreach (var layerPenalty in authoring.movementPenaltyLayers)
            {
                penaltyBuffer.Add(new MovementPenaltyElement
                {
                    LayerMaskValue = layerPenalty.layerMask.value,
                    Penalty = layerPenalty.penalty
                });
            }
            
            AddComponent(entity, new PathfindingGridSingleton
            {
                gridConfig =
                    new PathfindingGridConfig
                    {
                        worldOffset = authoring.transform.position,
                        width = width,
                        height = height,
                        nodeSize = nodeDiameter,
                        nodeRadius = authoring.nodeRadius,
                        gridBuildHeight = authoring.gridBuildHeight,
                        walkableLayers = authoring.walkableLayers.value,
                    }
            });
        }
    }
}


public struct PathfindingGridSingleton : IComponentData
{
    public PathfindingGridConfig gridConfig;
}

public struct PathfindingGridConfig
{
    public float3 worldOffset;
    public int width;
    public int height;
    public float nodeSize;
    public float nodeRadius;
    public float gridBuildHeight;
    public int walkableLayers;
}

public struct PathfindingGrid : IComponentData, IDisposable
{
    public float3 worldOffset;
    public int width;
    public int height;
    public float nodeSize;
    public float nodeRadius;
    public float gridBuildHeight;
    public int walkableLayers;
    public NativeArray<Node> grid;

    public float3 GetNodePosition(int x, int y) {
        float yPos = grid[y * width + x].yPosition;

        return new float3 {
            x = worldOffset.x - (nodeSize * width / 2) + (x * nodeSize) + (nodeSize / 2),
            y = worldOffset.y + yPos,
            z = worldOffset.z - (nodeSize * height / 2) + (y * nodeSize) + (nodeSize / 2)
        };
    }

    public float3 GetNodePosition (int2 index) {
        return GetNodePosition(index.x, index.y);
    }

    public Node GetNode(int x, int y) {
        return grid[y * width + x];
    }

    public PathfindingGrid Copy (Allocator allocator = Allocator.TempJob) {
        PathfindingGrid newGrid = this;
        newGrid.grid = new NativeArray<Node>(grid.Length, allocator);
        grid.CopyTo(newGrid.grid);
        return newGrid;
    }

    public int2 GetNodeIndex(float3 worldPosition) {
        float3 localPos = worldPosition - worldOffset;

        int rx = (int)((localPos.x / nodeSize) + (width / 2));
        int ry = (int)((localPos.z / nodeSize) + (height / 2));

        if(rx < 0 || rx >= width || ry < 0 || ry >= height) {
            return new int2 { x = -1, y = -1 };
        }

        return new int2 { x = rx, y = ry };
    }
    
    public void Dispose () {
        if (grid.IsCreated) {
            grid.Dispose();
        }
    }
}

public struct Node
{
    public bool walkable;
    public float yPosition;
    public int movementPenalty;
}

public struct MovementPenaltyElement : IBufferElementData
{
    public int LayerMaskValue;
    public int Penalty;
}