using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct CalculateFormationJob : IJob
{
    public float3 mainTargetPosition;
    public NativeList<int> selectedUnitIndices;
    public float spacing; // 유닛 간격

    public NativeArray<float3> allUnitTargetPositions; // output
    
    public void Execute()
    {
        if (selectedUnitIndices.Length == 0) return; // selectedUnit이 없는 경우
        
        int gridWidth = (int)math.ceil(math.sqrt(selectedUnitIndices.Length));

        for (int i = 0; i < selectedUnitIndices.Length; i++)
        {
            int unitIndex = selectedUnitIndices[i];

            int x = i % gridWidth;
            int y = i / gridWidth;
            
            float3 offset = new float3(
                (x - (gridWidth - 1) * 0.5f) * spacing,
                0,
                (y - (gridWidth - 1) * 0.5f) * spacing
            );
            
            allUnitTargetPositions[unitIndex] = mainTargetPosition + offset;
        }
    }
}