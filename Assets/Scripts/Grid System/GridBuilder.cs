using System;
using System.Collections.Generic;
using Unity.Burst;
using UnityEngine;
using UnityEngine.Serialization;

public class GridBuilder : MonoBehaviour
{
	public Vector2Int gridSize;
	private int width;
	private int height;
	public float gridHeight = 10f;
	public float nodeRadius = 0.5f;
	private float nodeDiam;
	public LayerMask walkableMask = -1;
	public LayerMask unwalkableMask = -1;
	public int obstacleProximityCost = 10;
	private int penaltyMax = int.MinValue;
	private int penaltyMin = int.MaxValue;
	public TerrainType[] walkableRegions;
	Dictionary<int, int> walkableRegionsDictionary = new Dictionary<int, int>();

	public static PathfindingGrid pathfindingGrid;

	void Start () {
		nodeDiam = nodeRadius * 2f;
		width = Mathf.CeilToInt(gridSize.x / nodeDiam);
		height = Mathf.CeilToInt(gridSize.y / nodeDiam);
		
		BuildGrid();
		PathFindingSystem.Instance.UpdateGrid(pathfindingGrid);
	}

	private void BuildGrid () {

		foreach (TerrainType region in walkableRegions)
		{
			walkableMask.value |= region.terrainMask.value;
			walkableRegionsDictionary.Add((int)Mathf.Log(region.terrainMask.value, 2),region.terrainPenalty);
		}

		pathfindingGrid = new PathfindingGrid();
		pathfindingGrid.width = width;
		pathfindingGrid.height = height;
		pathfindingGrid.grid = new Unity.Collections.NativeArray<Node>(width * height, Unity.Collections.Allocator.Persistent);
		pathfindingGrid.nodeSize = nodeDiam;

		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++)
			{
				Vector3 worldPoint = pathfindingGrid.GetNodePosition(x, y);
				bool walkable = !(Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask));
				int moveCost = 0;

				if (walkable)
				{
					Ray ray = new Ray(worldPoint + Vector3.up * 50, Vector3.down);
					RaycastHit hit;
					if (Physics.Raycast(ray, out hit, 100, walkableMask))
					{
						walkableRegionsDictionary.TryGetValue(hit.collider.gameObject.layer, out moveCost);
					}
				}
				else
				{
					moveCost += obstacleProximityCost;
				}

				Node node = new Node{
					walkable = walkable,
					moveCost = moveCost,
				};
				pathfindingGrid.grid[y * width + x] = node;
			}
		}

		BlurCostMap(5);
	}

	void BlurCostMap(int blurSize)
	{
		int kernelSize = blurSize * 2 + 1;
		int kernelExtents = (kernelSize - 1) / 2;

		int[,] penaltiesHorizontalPass = new int[width, height];
		int[,] penaltiesVerticalPass = new int[width, height];

		for (int y = 0; y < height; y++)
		{
			for (int x = -kernelExtents; x <= kernelExtents; x++)
			{
				int sampleX = Mathf.Clamp(x, 0, kernelExtents);
				penaltiesHorizontalPass[0, y] += pathfindingGrid.grid[y * width + sampleX].moveCost;
			}

			for (int x = 1; x < width; x++)
			{
				int removeIndex = Mathf.Clamp(x - kernelExtents -1, 0, width);
				int addIndex = Mathf.Clamp(x + kernelExtents, 0, width - 1);
				penaltiesHorizontalPass[x, y] = penaltiesHorizontalPass[x - 1, y] 
					- pathfindingGrid.grid[y * width + removeIndex].moveCost 
					+ pathfindingGrid.grid[y * width + addIndex].moveCost;
			}
		}
		
		for (int x = 0; x < width; x++) {
			for (int y = -kernelExtents; y <= kernelExtents; y++) {
				int sampleY = Mathf.Clamp (y, 0, kernelExtents);
				penaltiesVerticalPass [x, 0] += penaltiesHorizontalPass [x, sampleY];
			}

			int blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass [x, 0] / (kernelSize * kernelSize));
			var node1 = pathfindingGrid.grid[0 * width + x];
			node1.moveCost = blurredPenalty;
			pathfindingGrid.grid[0 * width + x] = node1;

			for (int y = 1; y < height; y++) {
				int removeIndex = Mathf.Clamp(y - kernelExtents - 1, 0, height);
				int addIndex = Mathf.Clamp(y + kernelExtents, 0, height-1);

				penaltiesVerticalPass [x, y] = penaltiesVerticalPass [x, y-1] - penaltiesHorizontalPass [x,removeIndex] + penaltiesHorizontalPass [x, addIndex];
				blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass [x, y] / (kernelSize * kernelSize));
				var node2 = pathfindingGrid.grid[y * width + x];
				node2.moveCost = blurredPenalty;
				pathfindingGrid.grid[y * width + x] = node2;

				if (blurredPenalty > penaltyMax) {
					penaltyMax = blurredPenalty;
				}
				if (blurredPenalty < penaltyMin) {
					penaltyMin = blurredPenalty;
				}
			}
		}
	}

	private void OnDrawGizmos () {
		if (Application.isPlaying)
		{
			// 색상 그라데이션 계산을 위한 페널티 최댓값입니다.
			// 게임에 설정된 가장 큰 movePenalty 값으로 조절해주세요.

			for (int x = 0; x < pathfindingGrid.width; x++)
			{
				for (int y = 0; y < pathfindingGrid.height; y++)
				{
					Node node = pathfindingGrid.GetNode(x, y);

					// 1. 기본 색상 설정
					if (!node.walkable)
					{
						// 이동 불가 노드: 빨간색
						Gizmos.color = new Color(1, 0, 0, 0.5f);
					}
					else
					{
						Gizmos.color = Color.Lerp(Color.white, Color.black, 
							Mathf.InverseLerp(penaltyMin, penaltyMax, node.moveCost));
						Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.7f); // 투명도 조절
					}

					Gizmos.DrawCube(pathfindingGrid.GetNodePosition(x, y), new Vector3(pathfindingGrid.nodeSize, 1f, pathfindingGrid.nodeSize));
				}
			}
		}
	}

	[System.Serializable]
	public class TerrainType
	{
		public LayerMask terrainMask;
		public int terrainPenalty;
	}
}