using System;
using System.Collections.Generic;
using Moko;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class PathFindingSystem : MonoBehaviour
{
    public static PathFindingSystem Instance;
    
    private List<PathFindingRequest> newRequests = new List<PathFindingRequest>();
    private List<PathFindingRequest> processingRequests = new List<PathFindingRequest>();

    private bool isJobRunning = false;
    
    private NativeArray<UnsafeList<int2>> processingResults;
    private NativeArray<float3> processingSrcPositions;
    private NativeArray<float3> processingDstPositions;
    
    private JobHandle jobHandle;
    public PathfindingGrid localGrid;

    private void Awake()
    {
        Instance = this;
    }

    private void LateUpdate()
    {
        if (isJobRunning)
        {
            // 처리중인 작업이 모두 끝난 경우
            if (jobHandle.IsCompleted)
            {
                jobHandle.Complete();

                if (processingRequests.Count > 0)
                {
                    for (int i = 0; i < processingRequests.Count; i++)
                    {
                        var originalRequest = processingRequests[i];
                        UnsafeList<int2> pathResult = processingResults[i];

                        List<Vector3> nodes = new List<Vector3>(pathResult.Length);
                        bool isFailed = false;
                        if (pathResult.Length > 0)
                        {
                            for (int j = pathResult.Length - 1; j >= 0; j--)
                            {
                                nodes.Add(localGrid.GetNodePosition(pathResult[j]));
                            }

                            nodes = SimplifyPath(nodes);
                        }
                        else
                        {
                            isFailed = true;
                        }
                        Path path = new Path(nodes, processingRequests[i].src, 3f)
                        {
                            failed = isFailed
                        };

                        originalRequest.result = path;
                        originalRequest.done = true;
                    }
                }

                processingSrcPositions.Dispose();
                processingDstPositions.Dispose();
                for (int i = 0; i < processingRequests.Count; i++)
                {
                    processingResults[i].Dispose();
                }
                processingResults.Dispose();
                processingRequests.Clear();
                
                isJobRunning = false;
            } // 처리중인 작업이 모두 끝난 경우
        }

        if (!isJobRunning && newRequests.Count > 0)
        {
            isJobRunning = true;
            
            (processingRequests, newRequests) = (newRequests, processingRequests);
            newRequests.Clear();

            int jobCount = processingRequests.Count;
            
            processingSrcPositions = new NativeArray<float3> (jobCount, Allocator.Persistent);
            processingDstPositions = new NativeArray<float3> (jobCount, Allocator.Persistent);
            processingResults = new NativeArray<UnsafeList<int2>> (jobCount, Allocator.Persistent);

            for (int i = 0; i < jobCount; i++)
            {
                processingSrcPositions[i] = processingRequests[i].src;
                processingDstPositions[i] = processingRequests[i].dst;
                processingResults[i] = new UnsafeList<int2>(256, Allocator.Persistent);
            }

            var job = new ProcessPathJob
            {
                grid = this.localGrid,
                srcPositions = processingSrcPositions,
                dstPositions = processingDstPositions,
                results = processingResults
            };

            jobHandle = job.Schedule(jobCount, 32);
        }
    }
    
    private List<Vector3> SimplifyPath(List<Vector3> path)
    {
        if (path.Count < 2)
        {
            return path;
        }

        List<Vector3> waypoints = new List<Vector3>();
        Vector3 directionOld = Vector3.zero;
    
        waypoints.Add(path[0]); 

        for (int i = 1; i < path.Count - 1; i++)
        {
            Vector3 directionNew = (path[i] - path[i-1]).normalized;
            if (Vector3.Dot(directionNew, directionOld) < 0.99f) 
            {
                waypoints.Add(path[i]);
            }
            directionOld = directionNew;
        }
    
        waypoints.Add(path[path.Count - 1]); 

        return waypoints;
    }


    public void QueueJob(PathFindingRequest request)
    {
        newRequests.Add(request);
    }

    public void UpdateGrid(PathfindingGrid sourceGrid) // 원본 그리드를 sourceGrid로 받음
    {
        // 로컬 그리드가 없거나 크기가 다르면 새로 생성/재할당
        if (!localGrid.grid.IsCreated || localGrid.grid.Length != sourceGrid.grid.Length)
        {
            if(localGrid.grid.IsCreated) localGrid.grid.Dispose();
        
            localGrid = new PathfindingGrid();
            localGrid.width = sourceGrid.width;
            localGrid.height = sourceGrid.height;
            localGrid.nodeSize = sourceGrid.nodeSize;
            localGrid.grid = new NativeArray<Node>(sourceGrid.grid.Length, Allocator.Persistent);
        }
    
        // 원본 데이터(sourceGrid)의 내용을 로컬 복사본(localGrid)으로 복사합니다.
        localGrid.grid.CopyFrom(sourceGrid.grid);
    }

    private void OnDestroy()
    {
        jobHandle.Complete();
        
        if (processingResults.IsCreated)
        {
            processingSrcPositions.Dispose();
            processingDstPositions.Dispose();
            for (int i = 0; i < processingResults.Length; i++)
            {
                processingResults[i].Dispose();
            }
            processingResults.Dispose();
        }
        
        this.localGrid.Dispose();
    }
}