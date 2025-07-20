using System.Collections;
using Moko;
using Unity.Mathematics;
using UnityEngine;

public class UnitPathFinder : MonoBehaviour
{
    private CharacterController controller;
    private PathFindingRequest currentRequest;
    private Vector3 targetPosition;
    private int currentPathIndex;
    public Path path { get; private set; }
    private Path currentPath;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
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
        }

        if (currentRequest != null && currentRequest.IsDone) // request를 올렸고, 끝난 경우
        {
            path = currentRequest.GetResult(); // path에 결과 저장
            currentRequest = null; // request 비우기
        }

        if (path != currentPath)
        {
            currentPath = path; // 현재 경로를 새로운 경로로 업데이트
            if (currentPath != null && !currentPath.failed)
            {
                StopAllCoroutines(); // 모든 이전 코루틴을 확실히 정지
                StartCoroutine(FollowPath(currentPath)); // 새로운 경로로 코루틴 시작
            }
        }

        /*if (path != null && !path.failed) // 경로가 존재하고 실패하지 않았을 때만 이동
        {
            if (currentPathIndex >= path.nodes.Count)
            {
                // 경로의 모든 노드를 통과했으므로 이동을 멈춥니다.
                path = null; // 경로 완료
                return;
            }

            Vector3 targetNodePosition = path.nodes[currentPathIndex];
            Vector3 moveDir = (targetNodePosition - GetPlayerPosition());
            moveDir.y = 0;
        
            if (moveDir.sqrMagnitude < 0.1f * 0.1f)
            {
                currentPathIndex++;
                return;
            }

            //controller.Move(moveDir.normalized * (Time.deltaTime * 15f));
            Quaternion targetRotation = Quaternion.LookRotation(path.nodes[currentPathIndex] - GetPlayerPosition());
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
            controller.Move(transform.forward.normalized * (Time.deltaTime * 10f));

            if (math.distancesq(GetPlayerPosition(), targetNodePosition) < 1.0f * 1.0f) 
            {
                currentPathIndex++;
            }
        }*/
    }

    private Vector3 GetPlayerPosition()
    {
        return new Vector3(transform.position.x, 0, transform.position.z);
    }

    IEnumerator FollowPath(Path pathToFollow)
    {
        bool followingPath = true;
        int pathIndex = 0;

        if (pathToFollow.nodes.Count == 0)
        {
            yield break;
        }

        while (followingPath)
        {
            Vector2 pos2D = new Vector2(transform.position.x, transform.position.z);
            if (pathToFollow.turnBoundaries[pathIndex].HasCrossedLine(pos2D))
            {
                pathIndex++;
            }
            
            if (pathIndex > pathToFollow.finishLineIndex) {
                followingPath = false;
                break;
            }
            
            // 이동 로직
            Vector3 targetNode = pathToFollow.nodes[pathIndex];
            Vector3 moveDirection = targetNode - transform.position;
            moveDirection.y = 0;

            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
            }
            controller.Move(transform.forward * (Time.deltaTime * 10f));

            yield return null;
        }
        
        currentPath = null;
        path = null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        /*if (rawPath != null && !rawPath.failed)
        {
            Gizmos.color = Color.black;

            foreach (var node in rawPath.nodes)
            {
                Gizmos.DrawCube(node, new Vector3(1f, 1f, 1f));
            }
        }*/
        
        if (path != null) path.DrawWithGizmos();
    }
#endif
}