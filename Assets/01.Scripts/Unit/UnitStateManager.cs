using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public class UnitStateManager : MonoBehaviour
{
    public static UnitStateManager Instance;
    
    private UnitDataManager dataManager;
    
    private JobHandle _lastFrameJobHandle;
    
    [Header("Properties")] 
    [SerializeField] public float formationNodeSpacing = 5f;
    [SerializeField] private float unitMoveSpeed = 15f;
    [SerializeField] private float unitRotationSpeed = 20f;
    [SerializeField] private float unitSeparationRadiusSq = 3f;
    [SerializeField] private float unitSeparationForce = 5f;
    

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        dataManager = UnitDataManager.Instance;
    }
    
    void LateUpdate()
    {
        _lastFrameJobHandle.Complete();
        
        JobHandle currentFrameJobHandle = ExecuteStateJob();
        currentFrameJobHandle.Complete();
        
        _lastFrameJobHandle = currentFrameJobHandle;
    }


    private JobHandle ExecuteStateJob()
    {
        var unitStateChangeCommandBuffer = new NativeList<UnitStateChangeCommand>(1000, Allocator.TempJob);
        var pathPositions = new NativeArray<float3>(dataManager.positions.Length, Allocator.TempJob);
        var hashMap = new NativeParallelMultiHashMap<int, int>(dataManager.positions.Length, Allocator.TempJob);
        var separationOffsets = new NativeArray<float3>(dataManager.positions.Length, Allocator.TempJob);

        
        var moveJob = new UnitMoveJob
        {
            stateChangeCommandBuffer = unitStateChangeCommandBuffer.AsParallelWriter(),
            
            unitState = dataManager.unitState,
            positions = dataManager.positions,
            nextPositions = pathPositions,
            rotations = dataManager.rotations,
            currentPathNodeIndex = dataManager.currentPathNodeIndex,
            pathLength = dataManager.pathLength,
            isAlive = dataManager.isAlive,
            pathDataStartIndex = dataManager.pathDataStartIndex,
            allPathNodes = dataManager.allPathNodes,
        
            deltaTime = Time.deltaTime,
            moveSpeed = unitMoveSpeed,
            rotationSpeed = unitRotationSpeed,
            waypointReachDistanceSq = 0.5f * 0.5f 
        };
        JobHandle moveJobHandle = moveJob.Schedule(dataManager.activeUnitCount, 64);


        var registerHashMapJob = new RegisterHashMapJob
        {
            isAlive = dataManager.isAlive,
            positions = dataManager.positions,
            cellSize = GridBuilder.pathfindingGrid.nodeSize,
            hashMap = hashMap.AsParallelWriter(),
        };
        JobHandle registerHandle = registerHashMapJob.Schedule(dataManager.activeUnitCount, 64);
        
        
        JobHandle seperationDependency = JobHandle.CombineDependencies(moveJobHandle, registerHandle);
        
        
        var separationJob = new UnitSeparationJob
        {
            unitState = dataManager.unitState,
            isAlive = dataManager.isAlive,
            positions = pathPositions,
            separationRadiusSq = unitSeparationRadiusSq,
            separationForce = unitSeparationForce,
            cellSize = GridBuilder.pathfindingGrid.nodeSize,
            deltaTime = Time.deltaTime,
            spatialHashMap = hashMap,
            separationOffsets = separationOffsets,
            pathLength = dataManager.pathLength,
        };
        JobHandle separationHandle = separationJob.Schedule(dataManager.activeUnitCount, 64, seperationDependency);


        var applyFinalPositionJob = new ApplyFinalPositionJob
        {
            unitState = dataManager.unitState,
            pathPositions = pathPositions,
            separationOffsets = separationOffsets,
            finalPositions = dataManager.positions 
        };
        JobHandle applyFinalPositionHandle = applyFinalPositionJob.Schedule(dataManager.activeUnitCount, 64, separationHandle);
        
        
        var transitionjob = new UnitStateTransitionJob()
        {
            unitAnimationID = dataManager.unitAnimationID,
            unitState = dataManager.unitState,
            unitStateChangeCommandBuffer = unitStateChangeCommandBuffer,
        };
        JobHandle transitionHandle = transitionjob.Schedule(applyFinalPositionHandle);
        
        
        dataManager.SyncTransforms();
        var syncJob = new SyncTransformsJob
        {
            positions = dataManager.positions,
            rotations = dataManager.rotations,
            isAlive = dataManager.isAlive
        };
        JobHandle syncHandle = syncJob.Schedule(dataManager.unitTransformAccessArray, transitionHandle);
        
        pathPositions.Dispose(syncHandle);
        hashMap.Dispose(syncHandle);
        separationOffsets.Dispose(syncHandle);
        unitStateChangeCommandBuffer.Dispose(syncHandle);
        
        return syncHandle;
    }
}
