using Unity.Mathematics;
using Unity.Burst;
using System.Runtime.CompilerServices;

public static class mathEx
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [BurstCompile]
    public static float3 ProjectOnPlane(float3 vector, float3 planeNormal)
    {
        float sqrMag = math.dot(planeNormal, planeNormal);
        if (sqrMag < math.EPSILON)
            return vector;
        else
        {
            var dot = math.dot(vector, planeNormal);
            return new float3(vector.x - planeNormal.x * dot / sqrMag,
                vector.y - planeNormal.y * dot / sqrMag,
                vector.z - planeNormal.z * dot / sqrMag);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [BurstCompile]
    public static float2 SmoothDamp(float2 current, float2 target, ref float2 currentVelocity, float smoothTime, float deltaTime)
    {
        float maxSpeed = float.PositiveInfinity;
        return SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [BurstCompile]
    public static float2 SmoothDamp(float2 current, float2 target, ref float2 currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
    {
        smoothTime = math.max(0.0001f, smoothTime);
        float num = 2f / smoothTime;
        float num2 = num * deltaTime;
        float num3 = 1f / (1f + num2 + 0.48f * num2 * num2 + 0.235f * num2 * num2 * num2);
        float num4 = current.x - target.x;
        float num5 = current.y - target.y;
        float2 f2 = target;
        float num6 = maxSpeed * smoothTime;
        float num7 = num6 * num6;
        float num8 = num4 * num4 + num5 * num5;
        if (num8 > num7)
        {
            float num9 = (float)math.sqrt(num8);
            num4 = num4 / num9 * num6;
            num5 = num5 / num9 * num6;
        }
        target.x = current.x - num4;
        target.y = current.y - num5;
        float num10 = (currentVelocity.x + num * num4) * deltaTime;
        float num11 = (currentVelocity.y + num * num5) * deltaTime;
        currentVelocity.x = (currentVelocity.x - num * num10) * num3;
        currentVelocity.y = (currentVelocity.y - num * num11) * num3;
        float num12 = target.x + (num4 + num10) * num3;
        float num13 = target.y + (num5 + num11) * num3;
        float num14 = f2.x - current.x;
        float num15 = f2.y - current.y;
        float num16 = num12 - f2.x;
        float num17 = num13 - f2.y;
        if (num14 * num16 + num15 * num17 > 0f)
        {
            num12 = f2.x;
            num13 = f2.y;
            currentVelocity.x = (num12 - f2.x) / deltaTime;
            currentVelocity.y = (num13 - f2.y) / deltaTime;
        }
        return new float2(num12, num13);
    }

    public static float2 LimitPrecision(float2 src, float precision)
    {
        return new float2(
            (int)(src.x * precision) / precision,
            (int)(src.y * precision) / precision
        );
    }
}

public static class QuaternionExtensions
{
    public static float EulerX(this quaternion q)
    {
        float sinr_cosp = 2 * (q.value.w * q.value.x + q.value.y * q.value.z);
        float cosr_cosp = 1 - 2 * (q.value.x * q.value.x + q.value.y * q.value.y);
        return math.atan2(sinr_cosp, cosr_cosp);
    }

    public static float EulerY(this quaternion q)
    {
        float sinp = 2 * (q.value.w * q.value.y - q.value.z * q.value.x);
        if (math.abs(sinp) >= 1)
            return math.PI / 2 * math.sign(sinp); // use 90 degrees if out of range
        else
            return math.asin(sinp);
    }

    public static float EulerZ(this quaternion q)
    {
        float siny_cosp = 2 * (q.value.w * q.value.z + q.value.x * q.value.y);
        float cosy_cosp = 1 - 2 * (q.value.y * q.value.y + q.value.z * q.value.z);
        return math.atan2(siny_cosp, cosy_cosp);
    }

    public static float3 Euler(this quaternion q)
    {
        return new float3(EulerX(q), EulerY(q), EulerZ(q));
    }

    public static quaternion FromEuler(float3 angles)
    {

        float cy = math.cos(angles.z * 0.5f);
        float sy = math.sin(angles.z * 0.5f);
        float cp = math.cos(angles.y * 0.5f);
        float sp = math.sin(angles.y * 0.5f);
        float cr = math.cos(angles.x * 0.5f);
        float sr = math.sin(angles.x * 0.5f);

        float4 q;
        q.w = cr * cp * cy + sr * sp * sy;
        q.x = sr * cp * cy - cr * sp * sy;
        q.y = cr * sp * cy + sr * cp * sy;
        q.z = cr * cp * sy - sr * sp * cy;

        return q;

    }
}