using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Entities;
using Unity.NetCode;

namespace Higo.Camera
{
    [UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
    [UpdateInGroup(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(CopyTransformToGameObjectSystem))]
    [UpdateAfter(typeof(EndFrameWorldToLocalSystem))]
    [AlwaysSynchronizeSystem]
    public class CopyToCameraSystem : ComponentSystem
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireSingletonForUpdate<CameraBrainComponent>();
        }

        protected override void OnUpdate()
        {
            var brainEnt = GetSingletonEntity<CameraBrainComponent>();
            var brain = GetSingleton<CameraBrainComponent>();


            if (Entity.Null == brain.CurrentCamera) return;

            var src = EntityManager.GetComponentData<LocalToWorld>(brain.CurrentCamera);
            var dst = EntityManager.GetComponentData<LocalToWorld>(brainEnt);

            var newPos = Vector3.Lerp(dst.Position, src.Position, 0.5f);
            var newRot = Quaternion.Slerp(dst.Rotation, src.Rotation, 0.5f);
            dst.Value = float4x4.TRS(newPos, newRot, Vector3.one);
            // dst.Value = src.Value;
            EntityManager.SetComponentData(brainEnt, dst);
        }
    }
}
