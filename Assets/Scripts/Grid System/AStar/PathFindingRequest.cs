using UnityEngine;

public class PathFindingRequest
{
    public Vector3 src;
    public Vector3 dst;

    internal bool done;
    internal Path result;

    public bool IsDone
    {
        get => done;
    }

    public PathFindingRequest(Vector3 start, Vector3 end)
    {
        this.src = start;
        this.dst = end;
    }

    public void Queue()
    {
        PathFindingSystem.instance.QueueJob(this);
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