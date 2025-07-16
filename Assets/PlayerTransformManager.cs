using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class PlayerTransformManager : MonoBehaviour
{
    public static PlayerTransformManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery entityQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<PlayerTag>().WithAll<LocalTransform>().Build(entityManager);

        NativeArray<LocalTransform> playerArray = entityQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);

        if (playerArray.Length == 1)
        {
            transform.position = playerArray[0].Position;
        }
        else
        {
            Debug.LogError("There are Two or more Player Objects in World");
        }
    }
}
