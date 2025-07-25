using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Serialization;

public class UnitStateManager : MonoBehaviour
{
    public static UnitStateManager Instance;
    
    private UnitDataManager dataManager;
    
    private JobHandle _lastFrameJobHandle;
    
    [Header("Properties")] 
    [SerializeField] public float formationNodeSpacing = 5f;
    [SerializeField] private float unitMoveSpeed = 15f;
    [SerializeField] private float unitRotationSpeed = 20f;
    [SerializeField] private float unitDirectionLerpSpeed = 5f;
    [SerializeField] private float unitSeparationRadius = 3f;
    [SerializeField] private float unitDirectionNoiseMagnitude = 1f;
    [FormerlySerializedAs("unitSeparationForce")] [SerializeField] private float unitSeparationWeight = 5f;
    

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
        var hashMap = new NativeParallelMultiHashMap<int, int>(dataManager.positions.Length, Allocator.TempJob);
        var desiredDirections = new NativeArray<float3>(dataManager.positions.Length, Allocator.TempJob);
        var nextPositions = new NativeArray<float3>(dataManager.positions.Length, Allocator.TempJob);

        
        var updatePathStateJob = new UpdatePathStateJob
        {
            unitState = dataManager.unitState,
            positions = dataManager.positions,
            isAlive = dataManager.isAlive,
            pathDataStartIndex = dataManager.pathDataStartIndex,
            allPathNodes = dataManager.allPathNodes,
            waypointReachDistanceSq = 0.5f * 0.5f, 
            currentPathNodeIndex = dataManager.currentPathNodeIndex,
            pathLength = dataManager.pathLength,
            stateChangeCommandBuffer = unitStateChangeCommandBuffer.AsParallelWriter()
        };
        JobHandle updatPathStateHandle = updatePathStateJob.Schedule(dataManager.activeUnitCount, 64);
        
        
        var calculatePathDirctionJob = new CalculatePathDirectionJob
        {
            unitState = dataManager.unitState,
            positions = dataManager.positions,
            currentPathNodeIndex = dataManager.currentPathNodeIndex,
            pathLength = dataManager.pathLength,
            isAlive = dataManager.isAlive,
            pathDataStartIndex = dataManager.pathDataStartIndex,
            allPathNodes = dataManager.allPathNodes,
            desiredDirections = desiredDirections,
        };
        JobHandle moveJobHandle = calculatePathDirctionJob.Schedule(dataManager.activeUnitCount, 64, updatPathStateHandle);

        
        var registerHashMapJob = new RegisterHashMapJob
        {
            isAlive = dataManager.isAlive,
            positions = dataManager.positions,
            cellSize = GridBuilder.pathfindingGrid.nodeSize,
            hashMap = hashMap.AsParallelWriter(),
        };
        JobHandle registerHandle = registerHashMapJob.Schedule(dataManager.activeUnitCount, 64, updatPathStateHandle);
        
        JobHandle seperationDependency = JobHandle.CombineDependencies(moveJobHandle, registerHandle);


        
        var separationJob = new CalculateSeparationDirectionJob
        {
            unitState = dataManager.unitState,
            positions = dataManager.positions,
            separationRadius = unitSeparationRadius,
            separationWeight = unitSeparationWeight, 
            spatialHashMap = hashMap,
            cellSize = GridBuilder.pathfindingGrid.nodeSize,
            desiredDirections = desiredDirections
        };
        JobHandle separationHandle = separationJob.Schedule(dataManager.activeUnitCount, 64, seperationDependency);

        
        var finalMoveJob = new FinalMoveJob
        {
            noiseMagnitude = unitDirectionNoiseMagnitude,
            randoms = dataManager.randoms,
            positions = dataManager.positions,
            desiredDirections = desiredDirections,
            moveSpeed = unitMoveSpeed,
            rotationSpeed = unitRotationSpeed,
            directionLerpSpeed = unitDirectionLerpSpeed, // 방향 전환 속도 (튜닝 필요)
            deltaTime = Time.deltaTime,
            velocities = dataManager.velocities, // [중요] 영구 데이터인 velocities 배열 전달
            rotations = dataManager.rotations,
            nextPositions = nextPositions
        };
        JobHandle finalMoveHandle = finalMoveJob.Schedule(dataManager.activeUnitCount, 64, separationHandle);

        
        var applyPositionJob = new MemCopyJob<float3>
        {
            Source = nextPositions,
            Destination = dataManager.positions,
        };
        JobHandle applyPositionHandle = applyPositionJob.Schedule(finalMoveHandle);
        
        var transitionjob = new UnitStateTransitionJob()
        {
            unitAnimationID = dataManager.unitAnimationID,
            unitState = dataManager.unitState,
            unitStateChangeCommandBuffer = unitStateChangeCommandBuffer,
        };
        JobHandle transitionHandle = transitionjob.Schedule(applyPositionHandle);
        
        
        dataManager.SyncTransforms();
        var syncJob = new SyncTransformsJob
        {
            positions = dataManager.positions,
            rotations = dataManager.rotations,
            isAlive = dataManager.isAlive
        };
        JobHandle syncHandle = syncJob.Schedule(dataManager.unitTransformAccessArray, transitionHandle);
        
        hashMap.Dispose(syncHandle);
        desiredDirections.Dispose(syncHandle);
        nextPositions.Dispose(syncHandle);
        unitStateChangeCommandBuffer.Dispose(syncHandle);
        
        return syncHandle;
    }
}
