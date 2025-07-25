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

    public int activeUnitCount { get; private set; } = 0;

    [Header("Base Prorperties")]
    public int maxUnits = 10000;
    public int maxPathNodes = 10000 * 50;
    public int maxStateChangeCommands = 10000 * 2; // maxUnits * unitStateCount
    public NativeArray<float3> positions;
    public NativeArray<quaternion> rotations;
    public NativeArray<float3> velocities;
    public NativeArray<bool> isSelected;
    public NativeArray<bool> isAlive;
    
    [Header("Pathfinding Properties")]
    public NativeArray<int> pathDataStartIndex;
    public NativeArray<int> pathLength;
    public NativeArray<int> currentPathNodeIndex;
    public NativeArray<float3> targetPosition; 
    public NativeList<float3> allPathNodes;
    public TransformAccessArray unitTransformAccessArray;
    public NativeArray<Unity.Mathematics.Random> randoms;
    
    [Header("StateMachine Prorperties")] 
    public NativeArray<UnitState> unitState;
    public NativeArray<int> unitAnimationID;
    
    public List<Unit> unitReferences;
    private Queue<int> availableIndices; // index pooling

    void Awake()
    {
        Instance = this;
        
        positions = new NativeArray<float3>(maxUnits, Allocator.Persistent);
        rotations = new NativeArray<quaternion>(maxUnits, Allocator.Persistent);
        velocities = new NativeArray<float3>(maxUnits, Allocator.Persistent);
        isSelected = new NativeArray<bool>(maxUnits, Allocator.Persistent);
        isAlive = new NativeArray<bool>(maxUnits, Allocator.Persistent);
        
        pathDataStartIndex = new NativeArray<int>(maxUnits, Allocator.Persistent);
        pathLength = new NativeArray<int>(maxUnits, Allocator.Persistent);
        currentPathNodeIndex = new NativeArray<int>(maxUnits, Allocator.Persistent);
        targetPosition = new NativeArray<float3>(maxUnits, Allocator.Persistent);
        allPathNodes = new NativeList<float3>(maxPathNodes, Allocator.Persistent);
        unitTransformAccessArray = new TransformAccessArray(maxUnits);
        randoms = new NativeArray<Unity.Mathematics.Random>(maxUnits, Allocator.Persistent);
        
        unitState = new NativeArray<UnitState>(maxUnits, Allocator.Persistent);
        unitAnimationID = new NativeArray<int>(maxUnits, Allocator.Persistent);

        unitReferences = new List<Unit>(maxUnits);
        availableIndices = new Queue<int>();

        for (int i = 0; i < maxUnits; i++)
        {
            randoms[i] = new Unity.Mathematics.Random((uint)(i + 1) * 0x9F6ABC1); 
            availableIndices.Enqueue(i);
            unitReferences.Add(null);
        }
    }

    void OnDestroy()
    {
        positions.Dispose();
        rotations.Dispose();
        velocities.Dispose();
        isSelected.Dispose();
        isAlive.Dispose();
        
        pathDataStartIndex.Dispose();
        pathLength.Dispose();
        currentPathNodeIndex.Dispose();
        targetPosition.Dispose();
        allPathNodes.Dispose();
        randoms.Dispose();
        
        unitState.Dispose();
        unitAnimationID.Dispose();
        
        unitTransformAccessArray.Dispose();
        
        Instance = null;
    }

    public int RegisterUnit(Unit unit)
    {
        if (availableIndices.Count == 0)
        {
            Debug.LogError("유닛 최대치 도달!");
            return -1;
        }

        activeUnitCount++;
        
        int index = availableIndices.Dequeue();

        positions[index] = unit.transform.position;
        rotations[index] = unit.transform.rotation;
        velocities[index] = float3.zero;
        isSelected[index] = false;
        isAlive[index] = true;

        unitState[index] = UnitState.Idle;
        unitAnimationID[index] = 0;

        unitReferences[index] = unit;
        
        return index;
    }

    public void UnregisterUnit(int index) // need to be updated
    {
        if (index < 0 || index >= isAlive.Length || !isAlive[index])
        {
            return; 
        }
     
        //Debug.Log($"{index}번째 유닛 unregister");
        
        activeUnitCount--;
        
        isAlive[index] = false;
        
        unitReferences[index] = null;

        availableIndices.Enqueue(index);
    }
    
    public void SyncTransforms()
    {
        var activeTransforms = unitReferences.Where(unit => unit != null)
            .Select(unit => unit.transform)
            .ToArray();
    
        unitTransformAccessArray.SetTransforms(activeTransforms);
    }
}
