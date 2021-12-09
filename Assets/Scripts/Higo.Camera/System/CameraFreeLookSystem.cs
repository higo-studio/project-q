using Unity.Entities;
using Unity.Mathematics;

namespace Higo.Camera
{
    [UpdateInGroup(typeof(CameraSystemGroup))]
    public class CameraFreeLookSystem : SystemBase
    {
        public const float Epsilon = CameraUtility.Epsilon;

        protected override void OnCreate()
        {
            RequireSingletonForUpdate<CameraBrainComponent>();
        }
        protected override void OnUpdate()
        {
            Entities.ForEach((
                ref CameraFreeLookParamComponent freeLookParam,
                in DynamicBuffer<CameraFreeLookCachedBuffer> freeLookCachedBuffer,
                in CameraActiveComponent activeState,
                in VirtualCameraComponent vcam) =>
            {
                if (activeState.Value != CameraActiveState.Running) return;
                // update axis

            }).Schedule();
        }

        float AxisGetMaxSpeed(ref CameraAxis axis)
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

        bool AxisMaxSpeedUpdate(ref CameraAxis axis, float input, float deltaTime)
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
            float MaxSpeed = AxisGetMaxSpeed(ref axis);
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