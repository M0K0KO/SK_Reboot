using Unity.Mathematics;

// used in CreatePathRequestJob
public struct PathfindingRequestData
{
    public int unitIndex;
    public float3 startPos;
    public float3 endPos;
}
