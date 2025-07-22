using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Mathematics;
using UnityEngine;

// 관리되는 모든 유닛들에게 명령을 내리는 컨트롤러
public class UnitCommandManager : MonoBehaviour
{
    public static UnitCommandManager Instance;
    
    private UnitDataManager dataManager;
    private Camera mainCam;

    private List<PathFindingRequest> pendingRequests = new List<PathFindingRequest>();

    [Header("Properties")] 
    [SerializeField] private float formationNodeSpacing = 2f;
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
        
        
        var hashMap = new NativeParallelMultiHashMap<int, int>(dataManager.positions.Length, Allocator.TempJob);
        var registerHashMapJob = new RegisterHashMapJob
        {
            isAlive = dataManager.isAlive,
            positions = dataManager.positions,
            cellSize = GridBuilder.pathfindingGrid.nodeSize,
            hashMap = hashMap.AsParallelWriter(),
        };
        JobHandle registerHandle = registerHashMapJob.Schedule(dataManager.positions.Length, 64, moveJobHandle);
        
        
        JobHandle seperationDependency = JobHandle.CombineDependencies(moveJobHandle, registerHandle);
        
        
        var separationOffsets = new NativeArray<float3>(dataManager.positions.Length, Allocator.TempJob);
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

    public void ExecuteMove()
    {
        var pathPositions = new NativeArray<float3>(dataManager.positions.Length, Allocator.TempJob);
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
        
        
        var hashMap = new NativeParallelMultiHashMap<int, int>(dataManager.positions.Length, Allocator.TempJob);
        var registerHashMapJob = new RegisterHashMapJob
        {
            isAlive = dataManager.isAlive,
            positions = dataManager.positions,
            cellSize = GridBuilder.pathfindingGrid.nodeSize,
            hashMap = hashMap.AsParallelWriter(),
        };
        JobHandle registerHandle = registerHashMapJob.Schedule(dataManager.positions.Length, 64, moveJobHandle);
        
        
        JobHandle seperationDependency = JobHandle.CombineDependencies(moveJobHandle, registerHandle);
        
        
        var separationOffsets = new NativeArray<float3>(dataManager.positions.Length, Allocator.TempJob);
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

        //Debug.Log("Deselect와 Select Job이 순서대로 완료되었습니다.");
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
    public void RequestUnitMove(float3 targetPos)
    {
        // 선택된 유닛 index 가져오기
        var selectedIndices = new NativeList<int>(10000, Allocator.TempJob);
        var findSelectedUnits = new FindSelectedUnitsJob
        {
            isSelected = dataManager.isSelected,
            selectedUnitIndices = selectedIndices.AsParallelWriter(),
        };
        JobHandle findSelectedUnitsHandle = findSelectedUnits.Schedule(dataManager.isSelected.Length, 64);
        findSelectedUnitsHandle.Complete();
        //Debug.Log($"{selectedIndices.Length} units selected.");
        
        // 그리드 포메이션 계산
        var formationJob = new CalculateFormationJob
        {
            mainTargetPosition = targetPos,
            selectedUnitIndices = selectedIndices,
            spacing = formationNodeSpacing,
            gridWorldOrigin = GridBuilder.pathfindingGrid.worldOffset,
            nodeSize = GridBuilder.pathfindingGrid.nodeSize,
            allUnitTargetPositions = dataManager.targetPosition,
        };

        JobHandle formationJobHandle = formationJob.Schedule();
        formationJobHandle.Complete();
        selectedIndices.Dispose();
        
    
        // pathfindRequest 제작
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

        // processPathjob이 돌아가도록 queue에 넣음
        for (int i = 0; i < pathRequests.Length; i++)
        {
            var requestData = pathRequests[i];
        
            var request = new PathFindingRequest(requestData.unitIndex, requestData.startPos, requestData.endPos);
        
            request.Queue();
            pendingRequests.Add(request); 
        }

        /*if (pathRequests.Length > 0)
        {
            Debug.Log($"{pathRequests.Length}개의 경로 탐색 요청을 생성했습니다.");
        }*/

        pathRequests.Dispose();
    }
    private void ProcessCompletedRequests()
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
    private void UpdateUnitPathData(PathFindingRequest request)
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
}
