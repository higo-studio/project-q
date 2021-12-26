using Unity.Entities;
using Unity.Mathematics;

namespace Higo.Camera
{
    [UpdateInGroup(typeof(CameraLateUpdateGroup))]
    public class CameraActivateSystem : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireSingletonForUpdate<CameraBrainComponent>();
        }
        protected override void OnUpdate()
        {
            var brainEnt = GetSingletonEntity<CameraBrainComponent>();
            var brain = EntityManager.GetComponentData<CameraBrainComponent>(brainEnt);

            var waiting = Entity.Null;
            var waitingMaxPriority = -1;
            var PostUpdateCommands = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
            Entities
                .WithAll<CameraActiveRequest>()
                .WithNone<CameraActiveComponent>()
                .ForEach((Entity ent, in VirtualCameraComponent vcam) =>
            {
                if (vcam.Priority >= waitingMaxPriority)
                {
                    waiting = ent;
                    waitingMaxPriority = vcam.Priority;
                }
                
                PostUpdateCommands.RemoveComponent<CameraActiveRequest>(ent);
                PostUpdateCommands.AddComponent<CameraActiveComponent>(ent);
                PostUpdateCommands.SetComponent(ent, new CameraActiveComponent()
                {
                    Value = CameraActiveState.Waiting
                });
            }).Run();

            if (Entity.Null == brain.CurrentCamera && Entity.Null == waiting)
            {
                Entities.ForEach((Entity entity, ref VirtualCameraComponent vcam) =>
                {
                    if (vcam.Priority > waitingMaxPriority)
                    {
                        waitingMaxPriority = vcam.Priority;
                        waiting = entity;
                    }
                }).Run();
            }

            if (Entity.Null != waiting)
            {
                if (Entity.Null == brain.CurrentCamera)
                {
                    brain.CurrentCamera = waiting;
                    PostUpdateCommands.SetComponent(brainEnt, brain);
                    PostUpdateCommands.SetComponent(waiting, new CameraActiveComponent()
                    {
                        Value = CameraActiveState.Running
                    });
                }
                else
                {
                    var curVCam = EntityManager.GetComponentData<VirtualCameraComponent>(brain.CurrentCamera);
                    if (waitingMaxPriority >= curVCam.Priority)
                    {
                        brain.CurrentCamera = waiting;
                        PostUpdateCommands.SetComponent(waiting, new CameraActiveComponent()
                        {
                            Value = CameraActiveState.Running
                        });
                        PostUpdateCommands.SetComponent(brainEnt, brain);
                    }
                }
            }

            PostUpdateCommands.Playback(EntityManager);
            PostUpdateCommands.Dispose();
        }
    }
}