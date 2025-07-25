using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct FinalMoveJob : IJobParallelFor
{
    [ReadOnly] public float noiseMagnitude;
    [ReadOnly] public NativeArray<float3> positions;
    [ReadOnly] public NativeArray<float3> desiredDirections; 
    [ReadOnly] public float moveSpeed;
    [ReadOnly] public float rotationSpeed;
    [ReadOnly] public float directionLerpSpeed; 
    [ReadOnly] public float deltaTime;
    
    public NativeArray<Unity.Mathematics.Random> randoms;
    public NativeArray<float3> velocities; 
    public NativeArray<quaternion> rotations;

    [WriteOnly] public NativeArray<float3> nextPositions;

    public void Execute(int index)
    {
        var random = randoms[index];
        
        float3 desiredDirection = desiredDirections[index];
        float3 currentVelocity = velocities[index];

        if (math.lengthsq(desiredDirection) > 0.001f)
        {
            desiredDirection += random.NextFloat3Direction() * noiseMagnitude;
            
            float3 newVelocity = math.lerp(currentVelocity, desiredDirection, directionLerpSpeed * deltaTime);
            newVelocity = math.normalizesafe(newVelocity);
            
            newVelocity.y = 0;

            nextPositions[index] = positions[index] + newVelocity * moveSpeed * deltaTime;
            rotations[index] = math.slerp(rotations[index], quaternion.LookRotation(newVelocity, math.up()), rotationSpeed * deltaTime);

            velocities[index] = newVelocity;
            randoms[index] = random;
        }
        else
        {
            velocities[index] = math.lerp(currentVelocity, float3.zero, directionLerpSpeed * deltaTime);
            nextPositions[index] = positions[index] + velocities[index] * moveSpeed * deltaTime;
        }
    }
}
