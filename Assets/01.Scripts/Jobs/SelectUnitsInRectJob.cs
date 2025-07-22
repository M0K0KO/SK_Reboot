using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct SelectUnitsInRectJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> unitPositions; 
    [ReadOnly] public NativeArray<bool> isAlive;         
    [ReadOnly] public Rect selectionRect;                 
    [ReadOnly] public float4x4 viewProjectionMatrix;      
    [ReadOnly] public float2 screenSize;

    [WriteOnly] public NativeArray<bool> isSelected;

    public void Execute(int index)
    {
        if (!isAlive[index])
        {
            isSelected[index] = false;
            return;
        }

        float3 worldPos = unitPositions[index];
        float4 clipPos = math.mul(viewProjectionMatrix, new float4(worldPos, 1));

        if (clipPos.w < 0)
        {
            isSelected[index] = false;
            return;
        }
        
        float3 ndcPos = clipPos.xyz / clipPos.w;
        
        float2 screenPos = new float2(
            (ndcPos.x + 1.0f) * 0.5f * screenSize.x,
            (ndcPos.y + 1.0f) * 0.5f * screenSize.y
        );

        bool inBounds = 
            screenPos.x >= selectionRect.xMin &&
            screenPos.x <= selectionRect.xMax &&
            screenPos.y >= selectionRect.yMin &&
            screenPos.y <= selectionRect.yMax;

        isSelected[index] = inBounds;
    }
}
