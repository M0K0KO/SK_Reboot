using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.PlayerLoop;

// 관리되는 모든 유닛들에게 명령을 내리는 컨트롤러
public class UnitCommandManager : MonoBehaviour
{
    public static UnitCommandManager Instance;

    private UnitDataManager dataManager;
    private Camera mainCam;

    private List<PathFindingRequest> pendingRequests = new List<PathFindingRequest>();

    private void Awake()
    {
        Instance = this;
        mainCam = Camera.main;
    }

    private void Start()
    {
        dataManager = UnitDataManager.Instance;
    }

    private void Update()
    {
        ProcessCompletedRequests();
    }

    // Selection, Deselection
    public void SelectUnitsInArea(Rect screenRect)
    {
        Rect selectionRect = NormalizeRect(screenRect);

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
    } // Helper Function for SelectUnitsInArea()

    public void DeselectAllUnits()
    {
        var deselectJob = new DeselectUnitsJob
        {
            isSelected = dataManager.isSelected,
        };

        JobHandle handle = deselectJob.Schedule(dataManager.isSelected.Length, 64);
        handle.Complete();

        //Debug.Log("Unit deselect job completed.");
    }
    // Selection, Deselection


    public void ProcessCompletedRequests()
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
    } // should be called in Update
    public void UpdateUnitPathData(PathFindingRequest request)
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
    } // Helper Functio for ProcessCompletedReqeusts()
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
        //Debug.Log($"{selectedIndices.Length} units selected.");

        // 그리드 포메이션 계산
        var formationJob = new CalculateFormationJob
        {
            mainTargetPosition = targetPos,
            selectedUnitIndices = selectedIndices,
            spacing = UnitStateManager.Instance.formationNodeSpacing,
            gridWorldOrigin = GridBuilder.pathfindingGrid.worldOffset,
            nodeSize = GridBuilder.pathfindingGrid.nodeSize,
            allUnitTargetPositions = dataManager.targetPosition,
        };

        JobHandle formationJobHandle = formationJob.Schedule(findSelectedUnitsHandle);


        // pathfindRequest 제작
        var pathRequests = new NativeList<PathfindingRequestData>(dataManager.activeUnitCount, Allocator.TempJob);

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

        JobHandle createRequestsHandle = createRequestsJob.Schedule(dataManager.activeUnitCount, 64, formationJobHandle);


        var changeStateToMoveJob = new ChangeStateToMoveJob
        {
            indicesToChange = selectedIndices.AsDeferredJobArray(),
            unitAnimationID = dataManager.unitAnimationID,
            unitState = dataManager.unitState,
        };
        
        JobHandle changeStateHandle = changeStateToMoveJob.Schedule(createRequestsHandle);
        changeStateHandle.Complete();
        
        
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

        changeStateHandle.Complete();
        
        pathRequests.Dispose();
        selectedIndices.Dispose();
    }
}