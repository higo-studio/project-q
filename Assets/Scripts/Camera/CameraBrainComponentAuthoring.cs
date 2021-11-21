using System;
using Unity.Entities;
using Unity.Entities.Editor;
using Unity.Mathematics;
using UnityEngine;

public struct CameraBrainComponent : IComponentData
{
    public Entity CurrentCamera;
}

[DisallowMultipleComponent]
class CameraBrainComponentAuthoring : MonoBehaviour {}

public class CameraBarinConversionSystem : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((Camera camera, CameraBrainComponentAuthoring a) => {
            AddHybridComponent(camera);
            var entity = GetPrimaryEntity(camera);
            DstEntityManager.AddComponent<CameraBrainComponent>(entity);
            DstEntityManager.SetName(entity, "CameraBrain");
        });
    }
}