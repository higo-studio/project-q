using Unity.Mathematics;
using Unity.Entities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace Higo.Camera
{
    public static class CameraUtility
    {
        public const float Epsilon = 0.0001f;
        public static void UpdateOrbits(in Orbit[] Orbits, float SplineCurvature,
            out float4[] cachedKnots, out float4[] cachedCtrl1, out float4[] cachedCtrl2)
        {
            float t = SplineCurvature;
            cachedKnots = new float4[5];
            cachedCtrl1 = new float4[5];
            cachedCtrl2 = new float4[5];
            cachedKnots[1] = new float4(0, Orbits[2].Height, -Orbits[2].Radius, 0);
            cachedKnots[2] = new float4(0, Orbits[1].Height, -Orbits[1].Radius, 0);
            cachedKnots[3] = new float4(0, Orbits[0].Height, -Orbits[0].Radius, 0);
            cachedKnots[0] = math.lerp(cachedKnots[1], float4.zero, t);
            cachedKnots[4] = math.lerp(cachedKnots[3], float4.zero, t);
            SplineHelpers.ComputeSmoothControlPoints(
                in cachedKnots, ref cachedCtrl1, ref cachedCtrl2);
        }

        public static float3 GetLocalPositionForCameraFromInput(float t, in DynamicBuffer<CameraFreeLookCachedBuffer> buffer)
        {
            int n = 1;
            if (t > 0.5f)
            {
                t -= 0.5f;
                n = 2;
            }

            return SplineHelpers.Bezier3(
                t * 2f,
                float4to3(buffer[n].knot),
                float4to3(buffer[n].ctrl1),
                float4to3(buffer[n].ctrl2),
                float4to3(buffer[n + 1].knot));
        }

        public static float3 GetLocalPositionForCameraFromInput(float t, in float4[] cachedKnots, in float4[] cachedCtrl1, in float4[] cachedCtrl2)
        {
            int n = 1;
            if (t > 0.5f)
            {
                t -= 0.5f;
                n = 2;
            }

            return SplineHelpers.Bezier3(
                t * 2f,
                float4to3(cachedKnots[n]),
                float4to3(cachedCtrl1[n]),
                float4to3(cachedCtrl2[n]),
                float4to3(cachedKnots[n + 1]));
        }

        public static float3 float4to3(float4 f)
        {
            return new float3(f[0], f[1], f[2]);
        }

        const float kLogNegligibleResidual = -4.605170186f; // == math.Log(kNegligibleResidual=0.01f);

        /// <summary>Get a damped version of a quantity.  This is the portion of the
        /// quantity that will take effect over the given time.</summary>
        /// <param name="initial">The amount that will be damped</param>
        /// <param name="dampTime">The rate of damping.  This is the time it would
        /// take to reduce the original amount to a negligible percentage</param>
        /// <param name="deltaTime">The time over which to damp</param>
        /// <returns>The damped amount.  This will be the original amount scaled by
        /// a value between 0 and 1.</returns>
        public static float Damp(float initial, float dampTime, float deltaTime)
        {
            if (dampTime < Epsilon || Mathf.Abs(initial) < Epsilon)
                return initial;
            if (deltaTime < Epsilon)
                return 0;
            float k = -kLogNegligibleResidual / dampTime; //DecayConstant(dampTime, kNegligibleResidual);
            return initial * (1 - Mathf.Exp(-k * deltaTime));
        }

#if UNITY_EDITOR
        public static void DrawCameraPath(float3 atPos, quaternion orient, in float4[] cachedKnots, in float4[] cachedCtrl1, in float4[] cachedCtrl2)
        {
            Matrix4x4 prevMatrix = Gizmos.matrix;
            Gizmos.matrix = float4x4.TRS(atPos, orient, new float3(1));

            const int kNumSteps = 20;
            Vector3 currPos = GetLocalPositionForCameraFromInput(0f, cachedKnots, cachedCtrl1, cachedCtrl2);
            for (int i = 1; i < kNumSteps + 1; ++i)
            {
                float t = (float)i / (float)kNumSteps;
                Vector3 nextPos = GetLocalPositionForCameraFromInput(t, cachedKnots, cachedCtrl1, cachedCtrl2);
                Gizmos.DrawLine(currPos, nextPos);
                currPos = nextPos;
            }
            Gizmos.matrix = prevMatrix;
        }

        public static void DrawCircleAtPointWithRadius(Vector3 point, Quaternion orient, float radius)
        {
            Matrix4x4 prevMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(point, orient, radius * Vector3.one);

            const int kNumPoints = 25;
            Vector3 currPoint = Vector3.forward;
            Quaternion rot = Quaternion.AngleAxis(360f / (float)kNumPoints, Vector3.up);
            for (int i = 0; i < kNumPoints + 1; ++i)
            {
                Vector3 nextPoint = rot * currPoint;
                Gizmos.DrawLine(currPoint, nextPoint);
                currPoint = nextPoint;
            }
            Gizmos.matrix = prevMatrix;
        }
#endif
    }

}