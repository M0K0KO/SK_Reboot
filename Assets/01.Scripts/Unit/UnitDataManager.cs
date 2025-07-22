using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;


// 모든 unit을 관리함.
public class UnitDataManager : MonoBehaviour
{
    
    public static UnitDataManager Instance;
    
    public NativeArray<float3> positions;
    public NativeArray<quaternion> rotations;
    public NativeArray<bool> isSelected;
    public NativeArray<bool> isAlive;
    
    [Header("Pathfinding")]
    public NativeArray<int> pathDataStartIndex;
    public NativeArray<int> pathLength;
    public NativeArray<int> currentPathNodeIndex;
    public NativeArray<float3> targetPosition; 
    public NativeList<float3> allPathNodes;
    
    public TransformAccessArray transformAccessArray;

    public List<Unit> unitReferences;
    private Queue<int> availableIndices;

    void Awake()
    {
        Instance = this;
        int maxUnits = 10000;
        int maxPathNodes = maxUnits * 50;
        
        positions = new NativeArray<float3>(maxUnits, Allocator.Persistent);
        rotations = new NativeArray<quaternion>(maxUnits, Allocator.Persistent);
        isSelected = new NativeArray<bool>(maxUnits, Allocator.Persistent);
        isAlive = new NativeArray<bool>(maxUnits, Allocator.Persistent);
        
        pathDataStartIndex = new NativeArray<int>(maxUnits, Allocator.Persistent);
        pathLength = new NativeArray<int>(maxUnits, Allocator.Persistent);
        currentPathNodeIndex = new NativeArray<int>(maxUnits, Allocator.Persistent);
        targetPosition = new NativeArray<float3>(maxUnits, Allocator.Persistent);

        allPathNodes = new NativeList<float3>(maxPathNodes, Allocator.Persistent);

        transformAccessArray = new TransformAccessArray(maxUnits);

        unitReferences = new List<Unit>(maxUnits);
        availableIndices = new Queue<int>();

        for (int i = 0; i < maxUnits; i++)
        {
            availableIndices.Enqueue(i);
            unitReferences.Add(null);
        }
    }

    void OnDestroy()
    {
        positions.Dispose();
        rotations.Dispose();
        isSelected.Dispose();
        isAlive.Dispose();
        
        pathDataStartIndex.Dispose();
        pathLength.Dispose();
        currentPathNodeIndex.Dispose();
        targetPosition.Dispose();
        allPathNodes.Dispose();
        
        transformAccessArray.Dispose();
        
        Instance = null;
    }

    public int RegisterUnit(Unit unit)
    {
        if (availableIndices.Count == 0)
        {
            Debug.LogError("유닛 최대치 도달!");
            return -1;
        }

        int index = availableIndices.Dequeue();

        positions[index] = unit.transform.position;
        rotations[index] = unit.transform.rotation;
        isSelected[index] = false;
        isAlive[index] = true;
        
        unitReferences[index] = unit;
        
        return index;
    }

    public void UnregisterUnit(int index)
    {
        if (index < 0 || index >= isAlive.Length || !isAlive[index])
        {
            return; 
        }
        
        isAlive[index] = false;
        
        unitReferences[index] = null;

        availableIndices.Enqueue(index);
    }
    
    public void SyncTransforms()
    {
        var activeTransforms = unitReferences.Where(unit => unit != null)
            .Select(unit => unit.transform)
            .ToArray();
    
        transformAccessArray.SetTransforms(activeTransforms);
    }
}
