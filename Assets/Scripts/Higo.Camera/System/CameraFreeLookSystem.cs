using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;

namespace Higo.Camera
{
    [UpdateInGroup(typeof(CameraLateUpdateGroup))]
    public class CameraFreeLookSystem : SystemBase
    {

        protected override void OnCreate()
        {
            RequireSingletonForUpdate<CameraBrainComponent>();
            RequireSingletonForUpdate<CameraControllerComponent>();
        }
        protected override void OnUpdate()
        {
            var controller = GetSingleton<CameraControllerComponent>();
            var deltaTime = Time.DeltaTime;
            var postCommandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.TempJob);
            Entities.WithoutBurst().ForEach((
                Entity e,
                ref CameraFreeLookParamComponent freeLookParam,
                in DynamicBuffer<CameraFreeLookCachedBuffer> freeLookCachedBuffer,
                in CameraActiveComponent activeState,
                in VirtualCameraComponent vcam) =>
            {
                if (activeState.Value != CameraActiveState.Running
                    || Entity.Null == vcam.Follow) return;
                // update axis
                freeLookParam.XAxis.AxisMaxSpeedUpdate(controller.Axis.x, deltaTime);
                freeLookParam.YAxis.AxisMaxSpeedUpdate(controller.Axis.y, deltaTime);
                var localPos = CameraUtility.GetLocalPositionForCameraFromInput(freeLookParam.YAxis.Value, freeLookCachedBuffer);
                var localTRS = float4x4.Translate(localPos);
                var parentTRS = float4x4.EulerYXZ(0, freeLookParam.XAxis.Value, 0);
                var targetL2W = GetComponent<LocalToWorld>(vcam.Follow);
                var l2w = float4x4.TRS(targetL2W.Position, quaternion.identity, new float3(1));
                var lookAtL2W = GetComponent<LocalToWorld>(vcam.LookAt);

                var worldTRS = math.mul(math.mul(l2w, parentTRS), localTRS);
                var worldPos = new float3(worldTRS.c3.x, worldTRS.c3.y, worldTRS.c3.z);
                var lookAtTRS = float4x4.LookAt(worldPos, lookAtL2W.Position, math.up());
                postCommandBuffer.SetComponent(e, new LocalToWorld()
                {
                    Value = lookAtTRS
                });
            }).Run();

            postCommandBuffer.Playback(EntityManager);
            postCommandBuffer.Dispose();
        }
    }
}