#ifndef UNIT_GPU_DATA_INCLUDED
#define UNIT_GPU_DATA_INCLUDED

// --- 1. 공유 구조체 정의 ---
struct AnimationMeta
{
    int animationID;
    int startFrameIndex;
    int frameCount;
    float playbackSpeed;
};

struct UnitData
{
    float3 position;
    float4 rotation;
    float animationFrame;
    int animationID;
    float3 padding;
};

StructuredBuffer<UnitData> _UnitDataBuffer;
StructuredBuffer<uint> _VisibleInstanceIndices;
StructuredBuffer<float4> _VertexAnimationBuffer;
StructuredBuffer<AnimationMeta> _AnimationMetaBuffer;
int _TotalVertexDataCount; 

float3 RotateVectorByQuaternion(float3 vec, float4 q)
{
    return vec + 2.0 * cross(q.xyz, cross(q.xyz, vec) + q.w * vec);
}

// --- 4. Shader Graph의 Custom Function 노드가 호출할 최종 함수 ---
void GetFinalVertexData_float(uint instanceID, uint VertexID, uint _VertexCount, out float3 WorldPosition, out float3 WorldNormal, out float3 DebugColor)
{
    uint unitIndex = _VisibleInstanceIndices[instanceID];
    UnitData unit = _UnitDataBuffer[unitIndex];
    AnimationMeta meta = _AnimationMetaBuffer[unit.animationID];
    
    uint absoluteFrameIndex = meta.startFrameIndex + (uint)unit.animationFrame;

    uint finalBufferIndex = (absoluteFrameIndex * _VertexCount) + VertexID;



    
    //float indexRatio = (float)finalBufferIndex / (float)_TotalVertexDataCount;
    //DebugColor = float3(indexRatio, 0, 0); // 인덱스가 유효하면 0(검정) ~ 1(빨강) 사이의 값
    //DebugColor = float3(meta.playbackSpeed / 2.0f, 0, 0); // playbackSpeed Debugging
    if (unit.animationID == 0)
    {
        DebugColor = float3(1, 1, 1); // ID 0 (Idle) = 흰색
    }
    else if (unit.animationID == 1)
    {
        DebugColor = float3(1, 0, 0); // ID 1 (Move) = 빨간색
    }
    else
    {
        DebugColor = float3(0, 0, 1); // 그 외 ID = 파란색
    }

    

    float3 animatedPos = _VertexAnimationBuffer[finalBufferIndex].xyz;

    animatedPos = float3(animatedPos.x, animatedPos.z, -animatedPos.y);
    
    float3 rotatedPos = RotateVectorByQuaternion(animatedPos, unit.rotation);
    
    WorldPosition = rotatedPos + unit.position;
    WorldNormal = float3(0, 1, 0); // 임시 값
}

#endif 