using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct ProcessPathJob : IJobParallelFor // The Job That actually calculates the Path
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

    [ReadOnly] public PathfindingGrid grid;

    [ReadOnly] public NativeArray<float3> srcPositions;

    [ReadOnly] public NativeArray<float3> dstPositions;


    public NativeArray<UnsafeList<int2>> results;

    public unsafe void Execute(int index)
    {
        NativeBinaryHeap<NodeCost> open = new NativeBinaryHeap<NodeCost>(10000, Allocator.Temp);
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

                        int newGCost = currentNode.gCost
                                       + NodeDistance(currentNode.idx, newIdx)
                                       + neighbor.moveCost;

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