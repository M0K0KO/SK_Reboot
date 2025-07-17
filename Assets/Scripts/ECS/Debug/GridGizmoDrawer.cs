using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class GridGizmoDrawer : MonoBehaviour
{
    public static readonly List<float3> WalkablePositions = new List<float3>();
    public static readonly List<float3> UnwalkablePositions = new List<float3>();
    public static readonly List<WeightDebug> WeightMap = new List<WeightDebug>();
    public static readonly List<float3> PathWaypoints = new List<float3>();
    public static float NodeSize;
    
    public static int MinPenalty = int.MaxValue;
    public static int MaxPenalty = int.MinValue;

    public bool DrawPath;
    public bool DrawWeightMap;

    private void OnDrawGizmos()
    {
        if (DrawWeightMap && WeightMap.Count > 0)
        {
            foreach (var node in WeightMap)
            {   
                Gizmos.color =Color.Lerp(Color.white, Color.black, Mathf.InverseLerp(MinPenalty, MaxPenalty, node.Weight));
                Gizmos.DrawCube(node.Position, Vector3.one * (NodeSize));
            }
        }

        foreach (var pos in UnwalkablePositions)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawCube(pos, Vector3.one * (NodeSize));
        }
        
        if (DrawPath)
        {
            Gizmos.color = new Color(0, 0, 0, 1f);
            foreach (var pos in PathWaypoints)
            {
                Gizmos.DrawCube(pos, new Vector3(NodeSize, 0.5f, NodeSize));
            }
        }
    }
}

public struct WeightDebug
{
    public int Weight;
    public float3 Position;
}