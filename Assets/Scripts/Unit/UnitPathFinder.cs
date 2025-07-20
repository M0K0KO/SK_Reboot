using Moko;
using Unity.Mathematics;
using UnityEngine;

public class UnitPathFinder : MonoBehaviour
{
    private CharacterController controller;

    private PathFindingRequest currentRequest;
    private Vector3 targetPosition;
    private Path path;
    private int currentPathIndex;

    private float targetPosDistance;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        targetPosition = transform.position;
        targetPosition.y = 0;
    }

    private void Update()
    {
        // 현재 이동중 아님
        // 마우스 클릭 위치와 현재 targetPosition 비교 후 필요시 업데이트 -> job할당 -> 이동 sequence
        
        // 현재 이동중
        // 현재 targetPosition이랑 다른 곳에 새로운 Input -> targetPosition 업데이트 -> 현재 path삭제하고 job할당
        
        int2 idx = GridBuilder.pathfindingGrid.GetNodeIndex(MouseScreenPosition.Instance.currentMousePosition);
        Vector3 newTargetPosition = GridBuilder.pathfindingGrid.GetNodePosition(idx.x, idx.y);
        newTargetPosition.y = 0;

        bool shouldChangePath = Vector3.Distance(targetPosition, newTargetPosition) > 0.5f; // 현재 targetPosition과 다른 곳인 경우

        if (shouldChangePath)
        {
            targetPosition = newTargetPosition;
            if (GridBuilder.pathfindingGrid.GetNode(idx.x, idx.y).walkable == false) return; // 걸을 수 없는 곳이면 스킵

            currentRequest =
                new PathFindingRequest(GetPlayerPosition(), targetPosition);
            currentRequest.Queue();

            currentPathIndex = 0;
            path = null;
        }

        if (currentRequest != null && currentRequest.IsDone) // request를 올렸고, 끝난 경우
        {
            path = currentRequest.GetResult(); // path에 결과 저장
            currentRequest = null; // request 비우기
            currentPathIndex = 0;
        }

        if (path != null && !path.failed) // 경로가 존재하고 실패하지 않았을 때만 이동
        {
            // 현재 경로 인덱스가 유효한지 확인
            if (currentPathIndex < path.nodes.Count)
            {
                Vector3 targetNodePosition = path.nodes[currentPathIndex];
                Vector3 moveDir = (targetNodePosition - transform.position);
                moveDir.y = 0;
                moveDir = moveDir.normalized;

                controller.Move(moveDir * (Time.deltaTime * 15f));

                float distanceToNodeSq = math.distancesq(GetPlayerPosition(), targetNodePosition);

                // 현재 노드에 충분히 가까워지면 다음 노드로
                if (distanceToNodeSq < 1.5f * 1.5f) // 거리 제곱 사용
                {
                    currentPathIndex++;
                }
            }
            else // 모든 경로 노드를 처리했거나 경로가 끝났을 때
            {
                // 최종 목적지에 거의 도달했는지 확인 (경로의 마지막 노드가 아닌 실제 targetPosition과 비교)
                float finalDistanceToTarget = Vector3.Distance(GetPlayerPosition(), targetPosition);

                if (finalDistanceToTarget < 0.4f) // 최종 목적지에 충분히 가까워지면 멈춤
                {
                    DebugExtension.BoldLog("목표 지점 도달 완료!");
                    path = null; // 경로 완료
                    currentPathIndex = 0; // 인덱스 초기화
                    // 필요하다면 유닛의 속도를 0으로 설정하는 등의 추가 로직
                }
                else
                {
                    // 경로의 마지막 노드에 도달했지만 최종 목표에는 아직 미치지 못했을 때
                    // 이 상황은 주로 마지막 노드와 targetPosition이 다른 경우 발생합니다.
                    // 이 경우 마지막 노드에서 targetPosition까지 직접 이동하도록 합니다.
                    Vector3 moveDir = (targetPosition - transform.position);
                    moveDir.y = 0;
                    moveDir = moveDir.normalized;
                    controller.Move(moveDir * (Time.deltaTime * 15f));
                }
            }
        }
    }

    private Vector3 GetPlayerPosition()
    {
        return new Vector3(transform.position.x, 0, transform.position.z);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (path != null && !path.failed)
        {
            Gizmos.color = Color.black;

            foreach (var node in path.nodes)
            {
                Gizmos.DrawCube(node, new Vector3(1f, 1f, 1f));
            }
        }
    }
#endif
}