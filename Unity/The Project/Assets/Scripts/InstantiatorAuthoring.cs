using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;
// The generator for authoring components doesn't do this conversion (GameObject to Entity) automagically
public class InstantiatorAuthoring : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public GameObject Prefab;
    public int Count;

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        // This will register the prefab to be converted too
        referencedPrefabs.Add(Prefab);
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Instantiator
        {
            Prefab = conversionSystem.GetPrimaryEntity(Prefab),
            Count = Count
        });
    }
}

public struct Instantiator : IComponentData
{
    public Entity Prefab;
    public int Count;
}

[AlwaysSynchronizeSystem]
public class InstantiatorSystem : JobComponentSystem
{
    EntityQuery m_Query;

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        // Unity.Mathematics.Random
        // Seed cannot be 0 (zero).
        var random = new Random((uint)Environment.TickCount);
        var entityList = new NativeList<Entity>(Allocator.TempJob);

        Entities
            .WithStoreEntityQueryInField(ref m_Query)
            .ForEach((Instantiator instantiator) => {
                    // This will set the Length and resize the internal array if needed
                    entityList.ResizeUninitialized(instantiator.Count);

                EntityManager.Instantiate(instantiator.Prefab, entityList);

                for (var entityIndex = 0; entityIndex < entityList.Length; entityIndex++)
                {
                    var entity = entityList[entityIndex];
                    var position = new float3() { x = UnityEngine.Random.Range(-2.5f, 2.5f), y = UnityEngine.Random.Range(2f, 20f), z = UnityEngine.Random.Range(-2.5f, 2.5f) };
                    EntityManager.SetComponentData(entity, new Translation
                    {
                        Value = random.NextFloat3Direction() * random.NextFloat(-2, 2)
                    });

                    EntityManager.SetComponentData(entity, new Rotation
                    {
                        Value = random.NextQuaternionRotation()
                    });
                }

            })
            .WithStructuralChanges()
            .WithoutBurst()
            .Run();

        entityList.Dispose();

        // That's a batch operation for all entities captures in the query and scale really well.
        EntityManager.DestroyEntity(m_Query);

        return inputDeps;
    }
}