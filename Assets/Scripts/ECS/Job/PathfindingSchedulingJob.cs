using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
[WithNone(typeof(PathfindingJobComponent))]
public partial struct PathfindingSchedulingJob : IJobEntity
{
    [ReadOnly] public PathfindingGrid pathfindingGrid;
    public EntityCommandBuffer.ParallelWriter ecb;

    private void Execute(Entity entity, [ChunkIndexInQuery] int jobChunkIndex, in PathRequest request)
    {
        if (request.requestStatus != RequestStatus.Requested)
        {
            return;
        }

        ecb.SetComponent(jobChunkIndex, entity, new PathRequest { 
            startPosition = request.startPosition, 
            endPosition = request.endPosition,
            requestStatus = RequestStatus.Processing 
        });

        var resultList = new NativeList<int2>(Allocator.Persistent);
        var job = new ProcessPathJob
        {
            srcPosition = request.startPosition,
            dstPosition = request.endPosition,
            grid = this.pathfindingGrid,
            result = resultList,
            open = new NativeBinaryHeap<ProcessPathJob.NodeCost>(
                (int)(pathfindingGrid.width * pathfindingGrid.height / (pathfindingGrid.nodeSize) / 2), Allocator.Persistent),
            closed = new NativeHashMap<int2, ProcessPathJob.NodeCost>(128, Allocator.Persistent)
        };
        
        JobHandle jobHandle = job.Schedule();

        ecb.AddComponent(jobChunkIndex, entity, new PathfindingJobComponent
        {
            jobHandle = jobHandle,
            pathResult = resultList
        });
    }
}