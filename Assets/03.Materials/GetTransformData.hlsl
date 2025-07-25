#if !defined(INSTANCING_BUFFERS_INCLUDED)
#define INSTANCING_BUFFERS_INCLUDED
StructuredBuffer<float3> _PositionBuffer;
StructuredBuffer<float4> _RotationBuffer;
StructuredBuffer<uint> _VisibleInstanceIndices;
#endif

void GetTransformData_float(float InstanceID, out float3 Position, out float4 Rotation)
{
    uint visibleIndex = _VisibleInstanceIndices[(uint)InstanceID];

    Position = _PositionBuffer[visibleIndex];
    Rotation = _RotationBuffer[visibleIndex];
}
