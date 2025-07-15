using System.Collections.Generic;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class Grid : MonoBehaviour
{
    public bool displayGridGizmos;
    
    public Vector2 GridWorldSize;
    public Node[,] NodeGrid;
    public int GridSizeX { get; private set; }
    public int GridSizeY { get; private set; }
    public float NodeRadius;
    public LayerMask ObstacleMask;
    private float _nodeDiameter;


    private void Awake()
    {
        Init();
    }
    
    private void Init()
    {
        _nodeDiameter = NodeRadius * 2;
        GridSizeX = Mathf.RoundToInt(GridWorldSize.x / _nodeDiameter);
        GridSizeY = Mathf.RoundToInt(GridWorldSize.y / _nodeDiameter);
        CreateGrid();
    }


    public int MaxSize
    {
        get { return GridSizeX * GridSizeY; }
    }
    
    
    private void CreateGrid()
    {
        NodeGrid = new Node[GridSizeX, GridSizeY];

        Vector3 worldBottomLeft = transform.position - Vector3.right * GridWorldSize.x / 2 - Vector3.forward * GridWorldSize.y / 2;

        for (int i = 0; i < GridSizeX; i++)
        {
            for (int j = 0; j < GridSizeY; j++)
            {
                Vector3 worldPoint = worldBottomLeft 
                                     + (i * _nodeDiameter + NodeRadius) * Vector3.right 
                                     + (j * _nodeDiameter + NodeRadius) * Vector3.forward;
                bool isBlocked = !Physics.CheckSphere(worldPoint, NodeRadius, ObstacleMask);
                NodeGrid[i, j] = new Node(worldPoint, isBlocked, i, j);
            }
        }
    }

    public List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                
                int neighborX = node.GridX + x;
                int neighborY = node.GridY + y;

                if (neighborX >= 0 && neighborX < GridSizeX && neighborY >= 0 && neighborY < GridSizeY) 
                    neighbors.Add(NodeGrid[neighborX, neighborY]);
            }
        }
        
        return neighbors;
    }
    
    public Node GetNodeFromWorldPoint(Vector3 worldPos)
    {
        float percentX = (worldPos.x + GridWorldSize.x / 2) / GridWorldSize.x;
        float percentY = (worldPos.z + GridWorldSize.y / 2) / GridWorldSize.y;
        
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);
        
        int x = Mathf.RoundToInt((GridSizeX-1) * percentX);
        int y = Mathf.RoundToInt((GridSizeY-1) * percentY);
        return NodeGrid[x, y];
    }


    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(GridWorldSize.x, 1, GridWorldSize.y));

        if (NodeGrid != null && displayGridGizmos)
        {
            foreach (Node n in NodeGrid)
            {
                Gizmos.color = (n.Walkable) ? Color.green : Color.red;
                Gizmos.DrawCube(n.WorldPosition, Vector3.one * (_nodeDiameter - .1f));
            }
        }
    }
}
