using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class GridGizmoDrawer : MonoBehaviour
{
    public static readonly List<float3> WalkablePositions = new List<float3>();
    public static readonly List<float3> UnwalkablePositions = new List<float3>();
    public static readonly List<float3> PathWaypoints = new List<float3>();
    public static float NodeSize;

    public bool DrawGizmos;

    private void OnDrawGizmos()
    {
        if (DrawGizmos)
        {

            Gizmos.color = new Color(0, 1, 0, 0.5f);
            foreach (var pos in WalkablePositions)
            {
                Gizmos.DrawCube(pos, new Vector3(NodeSize * 0.95f, 0.5f, NodeSize * 0.95f));
            }

            Gizmos.color = new Color(1, 0, 0, 0.5f);
            foreach (var pos in UnwalkablePositions)
            {
                Gizmos.DrawCube(pos, new Vector3(NodeSize * 0.95f, 0.5f, NodeSize * 0.95f));
            }

            Gizmos.color = new Color(0, 0, 0, 1f);
            foreach (var pos in PathWaypoints)
            {
                Gizmos.DrawCube(pos, new Vector3(NodeSize * 0.95f, 0.5f, NodeSize * 0.95f));
            }
        }
    }
}
