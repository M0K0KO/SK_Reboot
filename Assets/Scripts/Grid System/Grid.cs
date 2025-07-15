using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class Grid : MonoBehaviour
{
    public Vector2 GridWorldSize;
    public Node[,] NodeGrid;
    public int GridXCnt { get; private set; }
    public int GridYCnt { get; private set; }
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
        GridXCnt = Mathf.RoundToInt(GridWorldSize.x / _nodeDiameter);
        GridYCnt = Mathf.RoundToInt(GridWorldSize.y / _nodeDiameter);
        CreateGrid();
    }

    private void CreateGrid()
    {
        NodeGrid = new Node[GridXCnt, GridYCnt];

        Vector3 worldBottomLeft = transform.position - Vector3.right * GridWorldSize.x / 2 - Vector3.forward * GridWorldSize.y / 2;

        for (int i = 0; i < GridXCnt; i++)
        {
            for (int j = 0; j < GridYCnt; j++)
            {
                Vector3 worldPoint = worldBottomLeft 
                                     + (i * _nodeDiameter + NodeRadius) * Vector3.right 
                                     + (j * _nodeDiameter + NodeRadius) * Vector3.forward;
                bool isBlocked = !Physics.CheckSphere(worldPoint, NodeRadius, ObstacleMask);
                NodeGrid[i, j] = new Node(worldPoint, isBlocked);
            }
        }
    }

    public Node GetNodeFromWorldPoint(Vector3 worldPos)
    {
        float percentX = (worldPos.x + GridWorldSize.x / 2) / GridWorldSize.x;
        float percentY = (worldPos.z + GridWorldSize.y / 2) / GridWorldSize.y;
        
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);
        
        int x = Mathf.RoundToInt((GridXCnt-1) * percentX);
        int y = Mathf.RoundToInt((GridYCnt-1) * percentY);
        return NodeGrid[x, y];
    }
    
    
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position,new Vector3(GridWorldSize.x,1,GridWorldSize.y));
        if (NodeGrid != null)
        {
            foreach(Node n in NodeGrid)
            {
                Gizmos.color = (n.Walkable) ? Color.green : Color.red;
                Gizmos.DrawCube(n.WorldPosition, Vector3.one * (_nodeDiameter-.1f));
            }
        }
    }
}
