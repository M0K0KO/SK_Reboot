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
    private PathfindingGrid grid;

    [BurstCompile]
    private struct ProcessPathJob : IJobParallelFor // The Job That actually calculates the Path
    {
        public struct NodeCost : IEquatable<NodeCost>, IComparable<NodeCost>
        {
            public int2 idx;
            public int gCost;
            public int hCost;
            public int2 origin;

            public NodeCost(int2 i, int2 origin)
            {
                this.idx = i;
                this.origin = origin;
                this.gCost = 0;
                this.hCost = 0;
            }

            public int CompareTo(NodeCost other)
            {
                int compare = fCost().CompareTo(other.fCost()); // compare a fCost;

                if (compare == 0)
                {
                    compare = hCost.CompareTo(other.hCost);
                }

                return -compare;
            }

            public bool Equals(NodeCost other)
            {
                var b = (this.idx == other.idx);

                return math.all(b);
            }

            public int fCost()
            {
                return gCost + hCost;
            }

            public override int GetHashCode()
            {
                return idx.GetHashCode();
            }
        }

        [ReadOnly]
        public PathfindingGrid grid;

        [ReadOnly]
        public NativeArray<float3> srcPositions;
        
        [ReadOnly]
        public NativeArray<float3> dstPositions;
        
        
        public NativeArray<UnsafeList<int2>> results;

        public unsafe void Execute(int index)
        {
            NativeBinaryHeap<NodeCost> open = new NativeBinaryHeap<NodeCost>(50000, Allocator.Temp);
            NativeHashMap<int2, NodeCost> closed = new NativeHashMap<int2, NodeCost>(50000, Allocator.Temp);
            
            int2 startNode = grid.GetNodeIndex(srcPositions[index]);
            int2 endNode = grid.GetNodeIndex(dstPositions[index]);

            if (startNode.x == -1 || endNode.x == -1)
            {
                return;
            }

            open.Add(new NodeCost(startNode, startNode));

            int2 boundsMin = new int2(0, 0);
            int2 boundsMax = new int2 { x = grid.width, y = grid.height };

            NodeCost currentNode = new NodeCost(startNode, startNode);

            while (open.Count > 0)
            {
                currentNode = open.RemoveFirst();

                if (!closed.TryAdd(currentNode.idx, currentNode))
                    break;

                if (math.all(currentNode.idx == endNode))
                {
                    break;
                }

                for (int xC = -1; xC <= 1; xC++)
                {
                    for (int yC = -1; yC <= 1; yC++)
                    {
                        int2 newIdx = currentNode.idx + new int2(xC, yC);

                        if (math.all(newIdx >= boundsMin & newIdx < boundsMax))
                        {
                            Node neighbor = grid.GetNode(newIdx.x, newIdx.y);

                            NodeCost newCost = new NodeCost(newIdx, currentNode.idx);

                            if (!neighbor.walkable || closed.TryGetValue(newIdx, out NodeCost _))
                            {
                                continue;
                            }

                            int newGCost = currentNode.gCost + NodeDistance(currentNode.idx, newIdx);

                            newCost.gCost = newGCost;
                            newCost.hCost = NodeDistance(newIdx, endNode);

                            int oldIdx = open.IndexOf(newCost);
                            if (oldIdx >= 0)
                            {
                                if (newGCost < open[oldIdx].gCost)
                                {
                                    open.RemoveAt(oldIdx);

                                    open.Add(newCost);
                                }
                            }
                            else
                            {
                                if (open.Count < open.Capacity)
                                {
                                    open.Add(newCost);
                                }

                                else
                                {
                                    open.Dispose();
                                    closed.Dispose();
                                    return;
                                }
                            }
                        }
                    }
                }
            } //while end


            var tempPath = new UnsafeList<int2>(256, Allocator.Temp);
            while (!math.all(currentNode.idx == currentNode.origin))
            {
                tempPath.Add(currentNode.idx);

                if (!closed.TryGetValue(currentNode.origin, out NodeCost next))
                {
                    open.Dispose();
                    closed.Dispose();
                    tempPath.Dispose();
                    return;
                }
                currentNode = next;
            }
            var resultList = results[index];
            resultList.AddRange(tempPath.Ptr, tempPath.Length);
            results[index] = resultList;
            
            tempPath.Dispose();
            open.Dispose();
            closed.Dispose();
        } //execute end


        private int NodeDistance(int2 nodeA, int2 nodeB)
        {
            int2 d = nodeA - nodeB;
            int distx = math.abs(d.x);
            int disty = math.abs(d.y);


            if (distx > disty)
                return 14 * disty + 10 * (distx - disty);
            else
                return 14 * distx + 10 * (disty - distx);
        }
    }


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

                        Path path = new Path();
                        if (pathResult.Length > 0)
                        {
                            path.nodes = new List<Vector3>(pathResult.Length);
                            for (int j = pathResult.Length - 1; j >= 0; j--)
                            {
                                path.nodes.Add(grid.GetNodePosition(pathResult[j]));
                            }
                        }
                        else
                        {
                            path.failed = true;
                        }

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
                grid = this.grid,
                srcPositions = processingSrcPositions,
                dstPositions = processingDstPositions,
                results = processingResults
            };

            jobHandle = job.Schedule(jobCount, 32);
        }
        
        /*if (requests.Count > 0 && this.grid.nodeSize > 0)
        {
            int jobCount = requests.Count;
            var srcPositions = new NativeArray<float3>(jobCount, Allocator.TempJob);
            var dstPositions = new NativeArray<float3>(jobCount, Allocator.TempJob);
            for (int i = 0; i < jobCount; i++)
            {
                srcPositions[i] = requests[i].src;
                dstPositions[i] = requests[i].dst;
            }
            
            var results = new NativeArray<UnsafeList<int2>>(jobCount, Allocator.TempJob);
            for (int i = 0; i < jobCount; i++)
            {
                results[i] = new UnsafeList<int2>(256, Allocator.TempJob);
            }

            job = new ProcessPathJob
            {
                grid = this.grid,
                srcPositions = srcPositions,
                dstPositions = dstPositions,
                results = results
            };

            jobHandle = job.Schedule(jobCount, 32);
            jobHandle.Complete();
            
            for (int i = 0; i < jobCount; i++)
            {
                var originalRequest = requests[i];
                UnsafeList<int2> pathResult = results[i];
                
                Path path = new Path();
                if(pathResult.Length > 0)
                {
                    path.nodes = new List<Vector3>(pathResult.Length);
                    for(int j = pathResult.Length - 1; j >= 0; j--)
                    {
                        path.nodes.Add(grid.GetNodePosition(pathResult[j]));
                    }
                }
                else
                {
                    path.failed = true;
                }
                originalRequest.result = path;
                originalRequest.done = true;
            }
            
            srcPositions.Dispose();
            dstPositions.Dispose();
    
            for (int i = 0; i < jobCount; i++)
            {
                results[i].Dispose();
            }
            results.Dispose();

            requests.Clear();
        }*/
    }

    public void QueueJob(PathFindingRequest request)
    {
        newRequests.Add(request);
    }

    public void UpdateGrid(PathfindingGrid grid)
    {
        this.grid.Dispose();

        if (grid.nodeSize > 0)
        {
            this.grid = grid;
        }
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
        
        this.grid.Dispose();
    }
}