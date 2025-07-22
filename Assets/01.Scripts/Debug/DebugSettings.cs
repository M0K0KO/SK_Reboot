using System;
using UnityEngine;

public class DebugSettings : MonoBehaviour
{
    public bool DrawDebug;

    [Header("Pathfind")] 
    public bool DrawPath;
    public bool DrawCostMap;
    public bool DrawPathfindingGrid;
    
    public static DebugSettings Instance;

    private void Awake()
    {
        Instance = this;

        if (DrawDebug == false)
        {
            DrawPath = false;
            DrawCostMap = false;
        }
    }
}
