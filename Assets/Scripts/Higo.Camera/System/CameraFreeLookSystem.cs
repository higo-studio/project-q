using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;

namespace Higo.Camera
{
    [UpdateInGroup(typeof(CameraLateUpdateGroup))]
    public class CameraFreeLookSystem : SystemBase
    {
        public const float Epsilon = CameraUtility.Epsilon;

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
                AxisMaxSpeedUpdate(ref freeLookParam.XAxis, controller.Axis.x, deltaTime);
                AxisMaxSpeedUpdate(ref freeLookParam.YAxis, controller.Axis.y, deltaTime);
                var localPos = CameraUtility.GetLocalPositionForCameraFromInput(freeLookParam.YAxis.Value, freeLookCachedBuffer);
                var localTRS = float4x4.Translate(localPos);
                var parentTRS = float4x4.EulerYXZ(0, freeLookParam.XAxis.Value, 0);
                var targetL2W = GetComponent<LocalToWorld>(vcam.Follow);
                var lookAtL2W = GetComponent<LocalToWorld>(vcam.LookAt);

                var worldTRS = math.mul(math.mul(targetL2W.Value, parentTRS), localTRS);
                var worldPos = new float3(worldTRS.c3.x, worldTRS.c3.y, worldTRS.c3.z);
                var lookAtTRS = float4x4.LookAt(worldPos, lookAtL2W.Position, math.up());
                postCommandBuffer.SetComponent(e, new LocalToWorld()
                {
                    Value = math.mul(worldTRS, lookAtTRS)
                });
            }).Run();

            postCommandBuffer.Playback(EntityManager);
            postCommandBuffer.Dispose();
        }

        [BurstCompile]
        static float AxisGetMaxSpeed(in CameraAxis axis)
        {
            float range = axis.MaxValue - axis.MinValue;
            if (!axis.Wrap && range > 0)
            {
                float threshold = range / 10f;
                if (axis.CurrentSpeed > 0 && (axis.MaxValue - axis.Value) < threshold)
                {
                    float t = (axis.MaxValue - axis.Value) / threshold;
                    return math.lerp(0, axis.MaxSpeed, t);
                }
                else if (axis.CurrentSpeed < 0 && (axis.Value - axis.MinValue) < threshold)
                {
                    float t = (axis.Value - axis.MinValue) / threshold;
                    return math.lerp(0, axis.MaxSpeed, t);
                }
            }
            return axis.MaxSpeed;
        }

        [BurstCompile]
        static bool AxisMaxSpeedUpdate(ref CameraAxis axis, float input, float deltaTime)
        {
            if (axis.MaxSpeed > Epsilon)
            {
                float targetSpeed = input * axis.MaxSpeed;
                if (math.abs(targetSpeed) < Epsilon
                    || (math.sign(axis.CurrentSpeed) == math.sign(targetSpeed)
                        && math.abs(targetSpeed) < math.abs(axis.CurrentSpeed)))
                {
                    // Need to decelerate
                    float a = math.abs(targetSpeed - axis.CurrentSpeed) / math.max(Epsilon, axis.DeaccelTime);
                    float delta = math.min(a * deltaTime, math.abs(axis.CurrentSpeed));
                    axis.CurrentSpeed -= math.sign(axis.CurrentSpeed) * delta;
                }
                else
                {
                    // Accelerate to the target speed
                    float a = math.abs(targetSpeed - axis.CurrentSpeed) / math.max(Epsilon, axis.AccelTime);
                    axis.CurrentSpeed += math.sign(targetSpeed) * a * deltaTime;
                    if (math.sign(axis.CurrentSpeed) == math.sign(targetSpeed)
                        && math.abs(axis.CurrentSpeed) > math.abs(targetSpeed))
                    {
                        axis.CurrentSpeed = targetSpeed;
                    }
                }
            }

            // Clamp our max speeds so we don't go crazy
            float MaxSpeed = AxisGetMaxSpeed(in axis);
            axis.CurrentSpeed = math.clamp(axis.CurrentSpeed, -MaxSpeed, MaxSpeed);

            axis.Value += axis.CurrentSpeed * deltaTime;
            bool isOutOfRange = (axis.Value > axis.MaxValue) || (axis.Value < axis.MinValue);
            if (isOutOfRange)
            {
                if (axis.Wrap)
                {
                    if (axis.Value > axis.MaxValue)
                        axis.Value = axis.MinValue + (axis.Value - axis.MaxValue);
                    else
                        axis.Value = axis.MaxValue + (axis.Value - axis.MinValue);
                }
                else
                {
                    axis.Value = math.clamp(axis.Value, axis.MinValue, axis.MaxValue);
                    axis.CurrentSpeed = 0f;
                }
            }
            return math.abs(input) > Epsilon;
        }
    }
}