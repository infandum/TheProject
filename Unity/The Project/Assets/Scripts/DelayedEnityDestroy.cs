using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;


[System.Serializable]
public struct DestroyEntityDelayed : IComponentData
{
    public float timeLeft;
}

public class DestroyEntityDelayedSystem : ComponentSystem
{ 
    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;

        Entities.ForEach((Entity entity, ref DestroyEntityDelayed entityDelayed) => {
            entityDelayed.timeLeft -= deltaTime;

            if (entityDelayed.timeLeft < 0f)
            {
                PostUpdateCommands.DestroyEntity(entity);
                Debug.Log($"entity destroyed: { entity }");
            }
        });

        //    for (int i = 0; i < group.Length; i++)
        //{
        //    //read next in group:
        //    var destroyDelayed = group.destroyDelayed[i];
        //    Entity entity = group.entity[i];

        //    //update time left:
        //    destroyDelayed.timeLeft -= deltaTime;
        //    group.destroyDelayed[i] = destroyDelayed;

        //    //destroy entity on time out:
        //    if (destroyDelayed.timeLeft < 0f)
        //    {
        //        PostUpdateCommands.DestroyEntity(entity);
        //        Debug.Log($"entity destroyed: { entity }");
        //    }
        //}
    }
}