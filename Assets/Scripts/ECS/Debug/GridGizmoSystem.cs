#if UNITY_EDITOR

using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
public partial class GridGizmoSystem : SystemBase 
{
    protected override void OnUpdate()
    {
        if (!SystemAPI.HasSingleton<PathfindingGrid>())
        {
            return;
        }
        
        var gridData = SystemAPI.GetSingleton<PathfindingGrid>();
        if (!gridData.grid.IsCreated)
        {
            return;
        }
        
        GridGizmoDrawer.WalkablePositions.Clear();
        GridGizmoDrawer.UnwalkablePositions.Clear();
        GridGizmoDrawer.NodeSize = gridData.nodeSize;

        for (int x = 0; x < gridData.width; x++)
        {
            for (int y = 0; y < gridData.height; y++)
            {
                var node = gridData.GetNode(x, y);
                float3 nodePosition = gridData.GetNodePosition(x, y);

                if (node.walkable)
                {
                    GridGizmoDrawer.WalkablePositions.Add(nodePosition);
                }
                else
                {
                    GridGizmoDrawer.UnwalkablePositions.Add(nodePosition);
                }
            }
        }
    }
}

#endif // UNITY_EDITOR