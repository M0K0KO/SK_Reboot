using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

public struct PathFinder : ICleanupComponentData
{
    public PathStatus status;
    public NativeList<int2> pathBuffer;
    public float3 currentTargetPosition;

    public JobHandle jobHandle;

    public void Dispose()
    {
        if (pathBuffer.IsCreated)
        {
            pathBuffer.Dispose();
        }
    }
}

public enum PathStatus
{
    Idle,       // 대기
    Requested,  // 경로 요청
    InProgress, // 경로 계산 중
    Ready,      // 경로 준비 완료
    Failed      // 경로 탐색 실패
}
