using System.Collections.Generic;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
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
    [FormerlySerializedAs("ObstacleMask")] public LayerMask UnwalkableMask;
    public LayerMask WalkableMask;
    Dictionary<int, int> walkableRegionsDictionary = new Dictionary<int, int>();
    private float _nodeDiameter;

    public TerrainType[] walkableRegions;
    public int obstacleProximityPenalty = 10;
    
    int penaltyMin = int.MaxValue;
    int penaltyMax = int.MinValue;
    
    private void Awake()
    {
        Init();
    }
    
    private void Init()
    {
        _nodeDiameter = NodeRadius * 2;
        GridSizeX = Mathf.RoundToInt(GridWorldSize.x / _nodeDiameter);
        GridSizeY = Mathf.RoundToInt(GridWorldSize.y / _nodeDiameter);

        foreach (TerrainType region in walkableRegions)
        {
            WalkableMask.value |= region.TerrainMask.value;
            walkableRegionsDictionary.Add((int)Mathf.Log(region.TerrainMask.value, 2), region.TerrainPenalty);
        }
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
                bool walkable = !Physics.CheckSphere(worldPoint, NodeRadius, UnwalkableMask);

                int movementPenalty = 0;

                Ray ray = new Ray(worldPoint + Vector3.up * 50, Vector3.down);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 100, WalkableMask))
                {
                    walkableRegionsDictionary.TryGetValue(hit.collider.gameObject.layer, out movementPenalty);
                }

                if (!walkable)
                {
                    movementPenalty += obstacleProximityPenalty;
                }
                
                NodeGrid[i, j] = new Node(worldPoint, walkable, i, j, movementPenalty);
            }
        }
        
        BlurPenaltyMap(3);
    }

    void BlurPenaltyMap(int blurSize)
    {
        int kernelSize = blurSize * 2 + 1;
        int kernelExtents = (kernelSize - 1) / 2;

        int[,] penaltiesHorizontalPass = new int[GridSizeX, GridSizeY];
        int[,] penaltiesVerticalPass = new int[GridSizeX, GridSizeY];

        for (int y = 0; y < GridSizeY; y++)
        {
            for (int x = -kernelExtents; x <= kernelExtents; x++)
            {
                int sampleX = Mathf.Clamp(x, 0, kernelExtents);
                penaltiesHorizontalPass[0, y] += NodeGrid[sampleX, y].movementPenalty;
            }

            for (int x = 1; x < GridSizeX; x++)
            {
                int removeIndex = Mathf.Clamp(x - kernelExtents - 1, 0, GridSizeX);
                int addIndex = Mathf.Clamp(x + kernelExtents, 0, GridSizeX - 1);

                penaltiesHorizontalPass[x, y] = penaltiesHorizontalPass[x - 1, y] -
                    NodeGrid[removeIndex, y].movementPenalty + NodeGrid[addIndex, y].movementPenalty;
            }
        }

        for (int x = 0; x < GridSizeX; x++)
        {
            for (int y = -kernelExtents; y <= kernelExtents; y++)
            {
                int sampleY = Mathf.Clamp(y, 0, kernelExtents);
                penaltiesVerticalPass[x, 0] += penaltiesHorizontalPass[x, sampleY];
            }

            int blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[x, 0] / (kernelSize * kernelSize));
            NodeGrid[x, 0].movementPenalty = blurredPenalty;

            for (int y = 1; y < GridSizeY; y++)
            {
                int removeIndex = Mathf.Clamp(y - kernelExtents - 1, 0, GridSizeY);
                int addIndex = Mathf.Clamp(y + kernelExtents, 0, GridSizeY - 1);

                penaltiesVerticalPass[x, y] = penaltiesVerticalPass[x, y - 1] -
                    penaltiesHorizontalPass[x, removeIndex] + penaltiesHorizontalPass[x, addIndex];
                blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[x, y] / (kernelSize * kernelSize));
                NodeGrid[x, y].movementPenalty = blurredPenalty;

                if (blurredPenalty > penaltyMax)
                {
                    penaltyMax = blurredPenalty;
                }

                if (blurredPenalty < penaltyMin)
                {
                    penaltyMin = blurredPenalty;
                }
            }
        }
        
        
        Debug.Log(penaltyMax + ": Max + Min :" + penaltyMin);
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
                Gizmos.color = Color.Lerp(Color.white, Color.black, Mathf.InverseLerp(penaltyMin, penaltyMax, n.movementPenalty));
                Gizmos.color = (n.Walkable) ? Gizmos.color : Color.red;
                Gizmos.DrawCube(n.WorldPosition, Vector3.one * (_nodeDiameter));
            }
        }
    }

    [System.Serializable]
    public class TerrainType
    {
        public LayerMask TerrainMask;
        [FormerlySerializedAs("terrainPenalty")] public int TerrainPenalty;
    }
}
