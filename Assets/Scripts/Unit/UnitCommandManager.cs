using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class UnitCommandManager : MonoBehaviour
{
    public static UnitCommandManager Instance;

    private UnitDataManager dataManager;

    private Camera mainCam;

    private List<PathFindingRequest> pendingRequests = new List<PathFindingRequest>();

    [Header("Properties")] 
    [SerializeField] private float unitSpacing = 2f;
    [SerializeField] private float unitMoveSpeed = 15f;
    [SerializeField] private float unitRotationSpeed = 20f;
    [SerializeField] private float unitSeparationRadiusSq = 2f;
    [SerializeField] private float unitSeparationForce = 2f;
    
    private void Awake()
    {
        Instance = this;
        mainCam = Camera.main;
    }

    private void Start()
    {
        dataManager = UnitDataManager.Instance;
    }

    void Update()
    {
        ProcessCompletedRequests();
    }

    void LateUpdate()
    {
        var pathPositions = new NativeArray<float3>(dataManager.positions.Length, Allocator.TempJob);
        var separationOffsets = new NativeArray<float3>(dataManager.positions.Length, Allocator.TempJob);
        var hashMap = new NativeParallelMultiHashMap<int, int>(dataManager.positions.Length, Allocator.TempJob);

        
        var moveJob = new UnitMoveJob
        {
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

        JobHandle moveJobHandle = moveJob.Schedule(dataManager.positions.Length, 64);
        
        
        
        
        var registerHashMapJob = new RegisterHashMapJob
        {
            isAlive = dataManager.isAlive,
            positions = dataManager.positions,
            cellSize = GridBuilder.pathfindingGrid.nodeSize,
            hashMap = hashMap.AsParallelWriter(),
        };
        JobHandle registerHandle = registerHashMapJob.Schedule(dataManager.positions.Length, 64, moveJobHandle);
        
        
        
        
        JobHandle seperationDependency = JobHandle.CombineDependencies(moveJobHandle, registerHandle);
        
        
        var separationJob = new UnitSeparationJob
        {
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
        JobHandle separationHandle = separationJob.Schedule(dataManager.positions.Length, 64, seperationDependency);



        var applyFinalPositionJob = new ApplyFinalPositionJob
        {
            pathPositions = pathPositions,
            separationOffsets = separationOffsets,
            finalPositions = dataManager.positions 
        };
        JobHandle applyFinalPositionHandle = applyFinalPositionJob.Schedule(dataManager.positions.Length, 64, separationHandle);
        
        
        dataManager.SyncTransforms();

        var syncJob = new SyncTransformsJob
        {
            positions = dataManager.positions,
            rotations = dataManager.rotations,
            isAlive = dataManager.isAlive
        };
        JobHandle syncHandle = syncJob.Schedule(dataManager.transformAccessArray, applyFinalPositionHandle);
        syncHandle.Complete();
        
        
        pathPositions.Dispose();
        hashMap.Dispose();
        separationOffsets.Dispose();
    }


    public void SelectUnitsInArea(Rect screenRect)
    {
        Rect selectionRect = NormalizeRect(screenRect);
        
        var dataManager = UnitDataManager.Instance;

        var deselectJob = new DeselectUnitsJob
        {
            isSelected = dataManager.isSelected
        };
        JobHandle deselectHandle = deselectJob.Schedule(dataManager.positions.Length, 64);

        var selectJob = new SelectUnitsInRectJob
        {
            unitPositions = dataManager.positions,
            isAlive = dataManager.isAlive,
            isSelected = dataManager.isSelected, 
            selectionRect = selectionRect,
            viewProjectionMatrix = mainCam.projectionMatrix * mainCam.worldToCameraMatrix,
            screenSize = new float2(Screen.width, Screen.height)
        };

        JobHandle selectHandle = selectJob.Schedule(dataManager.positions.Length, 64, deselectHandle);

        selectHandle.Complete(); 

        Debug.Log("Deselect와 Select Job이 순서대로 완료되었습니다.");
    }
    
    private Rect NormalizeRect(Rect rect)
    {
        if (rect.width < 0)
        {
            rect.x += rect.width;
            rect.width = -rect.width;
        }
        if (rect.height < 0)
        {
            rect.y += rect.height;
            rect.height = -rect.height;
        }
        return rect;
    }

    public void DeselectAllUnits()
    {
        var deselectJob = new DeselectUnitsJob
        {
            isSelected = dataManager.isSelected,
        };
        
        JobHandle handle = deselectJob.Schedule(dataManager.isSelected.Length, 64);
        handle.Complete();
        
        Debug.Log("Unit deselect job completed.");
    }
    
    public void MoveUnits(float3 targetPos)
    {
        // Get Selected Units
        var selectedIndices = new NativeList<int>(10000, Allocator.TempJob);
        var findSelectedUnits = new FindSelectedUnitsJob
        {
            isSelected = dataManager.isSelected,
            selectedUnitIndices = selectedIndices.AsParallelWriter(),
        };
        JobHandle findSelectedUnitsHandle = findSelectedUnits.Schedule(dataManager.isSelected.Length, 64);
        findSelectedUnitsHandle.Complete();
        Debug.Log($"{selectedIndices.Length} units selected.");
        
        // Calculate Formation
        var formationJob = new CalculateFormationJob
        {
            mainTargetPosition = targetPos,
            selectedUnitIndices = selectedIndices,
            spacing = unitSpacing,
            allUnitTargetPositions = dataManager.targetPosition,
        };

        JobHandle formationJobHandle = formationJob.Schedule();
        formationJobHandle.Complete();
        selectedIndices.Dispose();
        
    
        // Pathfind
        var pathRequests = new NativeList<PathfindingRequestData>(dataManager.positions.Length, Allocator.TempJob);

        var createRequestsJob = new CreatePathRequestsJob
        {
            isAlive = dataManager.isAlive,
            isSelected = dataManager.isSelected,
            positions = dataManager.positions,
            targetPositions = dataManager.targetPosition,
            grid = GridBuilder.pathfindingGrid,
            pathRequests = pathRequests.AsParallelWriter(),
            
            pathLength = dataManager.pathLength,
            pathDataStartIndex = dataManager.pathDataStartIndex,
            allPathNodes = dataManager.allPathNodes,
        };
    
        JobHandle handle = createRequestsJob.Schedule(dataManager.positions.Length, 64);
        handle.Complete();

        for (int i = 0; i < pathRequests.Length; i++)
        {
            var requestData = pathRequests[i];
        
            var request = new PathFindingRequest(requestData.unitIndex, requestData.startPos, requestData.endPos);
        
            request.Queue();
            pendingRequests.Add(request); 
        }

        if (pathRequests.Length > 0)
        {
            Debug.Log($"{pathRequests.Length}개의 경로 탐색 요청을 생성했습니다.");
        }

        pathRequests.Dispose();
    }
    
    void ProcessCompletedRequests()
    {
        for (int i = pendingRequests.Count - 1; i >= 0; i--)
        {
            var request = pendingRequests[i];

            if (request.done)
            {
                if (request.result != null && !request.result.failed)
                {
                    UpdateUnitPathData(request);
                }

                pendingRequests.RemoveAt(i);
            }
        }
    }
    
    void UpdateUnitPathData(PathFindingRequest request)
    {
        int unitIndex = request.unitIndex;
        List<Vector3> nodes = request.result.nodes;

        dataManager.pathLength[unitIndex] = 0;

        int pathStart = dataManager.allPathNodes.Length;
        foreach (var node in nodes)
        {
            dataManager.allPathNodes.Add(node);
        }

        dataManager.pathDataStartIndex[unitIndex] = pathStart;
        dataManager.pathLength[unitIndex] = nodes.Count;
        dataManager.currentPathNodeIndex[unitIndex] = 0; 
    }

    public void UpdateTargetPos(float3 targetPos)
    {
        var updateTargetPositionsJob = new UpdateTargetPositionsJob()
        {
            isSelected = dataManager.isSelected,
            targetPosition = dataManager.targetPosition,
            newPos = targetPos
        };
        
        JobHandle handle = updateTargetPositionsJob.Schedule(dataManager.isSelected.Length, 64);
        handle.Complete();
        
        Debug.Log("Update Target Position job completed.");
    }
}
