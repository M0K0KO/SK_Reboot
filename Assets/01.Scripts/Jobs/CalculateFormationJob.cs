using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct CalculateFormationJob : IJob
{
    public float3 mainTargetPosition;
    public NativeList<int> selectedUnitIndices;
    public float spacing; // 유닛 간 노드 간격
    public float3 gridWorldOrigin;
    public float nodeSize;

    public NativeArray<float3> allUnitTargetPositions; // output
    
    public void Execute()
    {
        if (selectedUnitIndices.Length == 0) return;

        float nodeRadius = nodeSize * 0.5f;

        // 앵커 노드
        int centerX = (int)math.floor((mainTargetPosition.x - gridWorldOrigin.x) / nodeSize);
        int centerY = (int)math.floor((mainTargetPosition.z - gridWorldOrigin.z) / nodeSize);

        int formationGridWidth = (int)math.ceil(math.sqrt(selectedUnitIndices.Length));
        
        for (int i = 0; i < selectedUnitIndices.Length; i++)
        {
            int unitIndex = selectedUnitIndices[i];

            int xOffset = i % formationGridWidth;
            int yOffset = i / formationGridWidth;
            
            int formationCenterXOffset = (int)math.floor((formationGridWidth - 1) * 0.5f);
            
            int targetNodeX = centerX + (xOffset - formationCenterXOffset) * (int)spacing;
            int targetNodeY = centerY + (yOffset - formationCenterXOffset) * (int)spacing;

            float3 targetWorldPos = new float3(
                gridWorldOrigin.x + targetNodeX * nodeSize + nodeRadius,
                mainTargetPosition.y, 
                gridWorldOrigin.z + targetNodeY * nodeSize + nodeRadius
            );

            allUnitTargetPositions[unitIndex] = targetWorldPos;
        }
    }
}