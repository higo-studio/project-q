using Higo.Camera;
using Unity.Entities;
using UnityEngine;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(CameraLateUpdateGroup))]
public class CameraAddRequestSystem : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<NetworkIdComponent>();
        RequireForUpdate(GetEntityQuery(ComponentType.ReadWrite<VirtualCameraComponent>()));
    }

    protected override void OnUpdate()
    {
        var localID = GetSingleton<NetworkIdComponent>().Value;
        var addCommand = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        Entities
            .WithNone<CameraActiveRequest, CameraActiveComponent>()
            .ForEach((Entity ent, in VirtualCameraComponent vcm, in Parent parent) =>
            {
                var ownID = EntityManager.GetComponentData<GhostOwnerComponent>(parent.Value).NetworkId;
                if(ownID == localID)
                {
                    addCommand.AddComponent<CameraActiveRequest>(ent);
                }
            }).WithoutBurst().Run();

        Entities
            .WithAll<CameraBrainComponent, LocalToWorld, Translation>()
            .WithNone<CopyTransformToGameObject>()
            .ForEach((Entity ent, in CameraBrainComponent brain) =>
            {
                if(brain.CurrentCamera != Entity.Null)
                {
                    addCommand.AddComponent<CopyTransformToGameObject>(ent);
                }
            }).WithoutBurst().Run();
        addCommand.Playback(EntityManager);
        addCommand.Dispose();
    }
}
