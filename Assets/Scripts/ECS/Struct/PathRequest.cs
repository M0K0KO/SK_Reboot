using Unity.Entities;
using Unity.Mathematics;

public struct PathRequest : IComponentData
{
    public float3 startPosition;
    public float3 endPosition;
    public RequestStatus requestStatus;
}

public enum RequestStatus
{
    Requested,
    Processing,
    Completed,
    Failed,
}