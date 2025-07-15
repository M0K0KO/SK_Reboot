using Unity.Mathematics;
using UnityEditor.Searcher;
using UnityEngine;

public class Node
{
    public Vector3 WorldPosition { get; private set; }
    public bool Walkable { get; private set; }

    public Node(Vector3 worldPos, bool walkable)
    {
        WorldPosition = worldPos;
        Walkable = walkable;
    }
    
    
    public void SetCenterPosition(Vector3 position) => WorldPosition = position;

    public void BlockNode() => Walkable = true;
    public void UnblockNode() => Walkable = false;
}