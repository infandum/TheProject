﻿/* 
    ------------------- Code Monkey -------------------

    Thank you for downloading this package
    I hope you find it useful in your projects
    If you have any questions let me know
    Cheers!

               unitycodemonkey.com
    --------------------------------------------------
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public class MoverSystem : ComponentSystem {

    protected override void OnUpdate() {
        Entities.ForEach((ref Translation translation, ref MoveSpeedComponent moveSpeedComponent) => {
            translation.Value.y += moveSpeedComponent.moveSpeed * Time.DeltaTime;
            if (translation.Value.y > 5f) {
                moveSpeedComponent.moveSpeed = -math.abs(moveSpeedComponent.moveSpeed);
            }
            if (translation.Value.y < -5f) {
                moveSpeedComponent.moveSpeed = +math.abs(moveSpeedComponent.moveSpeed);
            }
        });
    }

}
