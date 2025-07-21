using UnityEngine;
using UnityEngine.UIElements;

public class PathFindingRequest
{
    public readonly int unitIndex;
    
    public readonly Vector3 src;
    public readonly Vector3 dst;

    internal bool done;
    internal Path result;

    public bool IsDone
    {
        get => done;
    }

    public PathFindingRequest(int unitIndex, Vector3 start, Vector3 end)
    {
        this.unitIndex = unitIndex;
        this.src = start;
        this.dst = end;
    }

    public void Queue()
    {
        PathFindingSystem.Instance.QueueJob(this);
    }

    public Path GetResult()
    {
        if (!done)
        {
            Debug.LogError("Path is not done yet. Please wait for the IsDone function to return true.");
        }

        return result;
    }
}