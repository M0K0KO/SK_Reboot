using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Path
{
    public List<Vector3> nodes; // 자기 위치부터 시작
    public Line[] turnBoundaries; // 0부터 확인해야함
    public int finishLineIndex;
    public bool failed = false;


    public Path(List<Vector3> waypoints, Vector3 startPos, float turnDst)
    {
        nodes = waypoints;
        turnBoundaries = new Line[nodes.Count];
        finishLineIndex = turnBoundaries.Length - 1;
        
        Vector2 previousPoint = V3ToV2(startPos);
        for (int i = 0; i < nodes.Count; i++)
        {
            Vector2 currentPoint = V3ToV2 (nodes[i]);
            Vector2 dirToCurrentPoint = (currentPoint - previousPoint).normalized;
            Vector2 turnBoundaryPoint = (i == finishLineIndex) ? currentPoint : currentPoint - dirToCurrentPoint * turnDst;
            turnBoundaries [i] = new Line (turnBoundaryPoint, previousPoint - dirToCurrentPoint * turnDst);
            previousPoint = turnBoundaryPoint;
        }
    }

    Vector3 V3ToV2(Vector3 v3)
    {
        return new Vector3(v3.x, v3.z);
    }

    #if UNITY_EDITOR
    public void DrawWithGizmos()
    {
        if (DebugSettings.Instance.DrawPath)
        {
            Gizmos.color = Color.black;
            foreach (Vector3 p in nodes)
            {
                Gizmos.DrawCube(p + Vector3.up, Vector3.one);
            }
        }
    }
    #endif
}