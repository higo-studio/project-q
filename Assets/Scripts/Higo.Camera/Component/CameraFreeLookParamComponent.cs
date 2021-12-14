using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using System;
namespace Higo.Camera
{
    [Serializable]
    public struct CameraAxis
    {
        public const float Epsilon = CameraUtility.Epsilon;
        [Range(0, 1)]
        public float Value;
        public float MinValue;
        public float MaxValue;
        public float AccelTime;
        public float DeaccelTime;
        public float MaxSpeed;
        public float CurrentSpeed;
        public bool Wrap;
        public bool Inverse;

        [BurstCompile]
        public float AxisGetMaxSpeed()
        {
            float range = MaxValue - MinValue;
            if (!Wrap && range > 0)
            {
                float threshold = range / 10f;
                if (CurrentSpeed > 0 && (MaxValue - Value) < threshold)
                {
                    float t = (MaxValue - Value) / threshold;
                    return math.lerp(0, MaxSpeed, t);
                }
                else if (CurrentSpeed < 0 && (Value - MinValue) < threshold)
                {
                    float t = (Value - MinValue) / threshold;
                    return math.lerp(0, MaxSpeed, t);
                }
            }
            return MaxSpeed;
        }

        [BurstCompile]
        public bool AxisMaxSpeedUpdate(float input, float deltaTime)
        {
            if (Inverse)
            {
                input *= -1;
            }
            if (this.MaxSpeed > Epsilon)
            {
                float targetSpeed = input * this.MaxSpeed;
                if (math.abs(targetSpeed) < Epsilon
                    || (math.sign(CurrentSpeed) == math.sign(targetSpeed)
                        && math.abs(targetSpeed) < math.abs(CurrentSpeed)))
                {
                    // Need to decelerate
                    float a = math.abs(targetSpeed - CurrentSpeed) / math.max(Epsilon, DeaccelTime);
                    float delta = math.min(a * deltaTime, math.abs(CurrentSpeed));
                    CurrentSpeed -= math.sign(CurrentSpeed) * delta;
                }
                else
                {
                    // Accelerate to the target speed
                    float a = math.abs(targetSpeed - CurrentSpeed) / math.max(Epsilon, AccelTime);
                    CurrentSpeed += math.sign(targetSpeed) * a * deltaTime;
                    if (math.sign(CurrentSpeed) == math.sign(targetSpeed)
                        && math.abs(CurrentSpeed) > math.abs(targetSpeed))
                    {
                        CurrentSpeed = targetSpeed;
                    }
                }
            }

            // Clamp our max speeds so we don't go crazy
            float MaxSpeed = AxisGetMaxSpeed();
            CurrentSpeed = math.clamp(CurrentSpeed, -MaxSpeed, MaxSpeed);

            Value += CurrentSpeed * deltaTime;
            bool isOutOfRange = (Value > MaxValue) || (Value < MinValue);
            if (isOutOfRange)
            {
                if (Wrap)
                {
                    if (Value > MaxValue)
                        Value = MinValue + (Value - MaxValue);
                    else
                        Value = MaxValue + (Value - MinValue);
                }
                else
                {
                    Value = math.clamp(Value, MinValue, MaxValue);
                    CurrentSpeed = 0f;
                }
            }
            return math.abs(input) > Epsilon;
        }
    }

    public struct CameraFreeLookParamComponent : IComponentData
    {
        public float SplineCurvature;
        public CameraAxis XAxis;
        public CameraAxis YAxis;
    }
}
