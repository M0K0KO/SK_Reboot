using Unity.Mathematics;
using UnityEditor.Searcher;
using UnityEngine;

public class Node : IHeapItem<Node>
{
    public Vector3 WorldPosition { get; private set; }
    public bool Walkable { get; private set; }

    public int GridX;
    public int GridY;
    public int movementPenalty;

    public int gCost;
    public int hCost;
    public Node parent;
    private int heapIndex;
    
    public Node(Vector3 worldPos, bool walkable, int gridX, int gridY, int penalty)
    {
        WorldPosition = worldPos;
        Walkable = walkable;
        GridX = gridX;
        GridY = gridY;
        movementPenalty = penalty;
    }
    
    public int fCost
    {
        get { return gCost + hCost; }
    }

    public int HeapIndex
    {
        get
        {
            return heapIndex;
        }
        set
        {
            heapIndex = value;
        }
    }

    public int CompareTo(Node nodeToCompare)
    {
        int compare = fCost.CompareTo(nodeToCompare.fCost);
        if (compare == 0)
        {
            compare = hCost.CompareTo(nodeToCompare.hCost);
        }

        return -compare;
    }
}