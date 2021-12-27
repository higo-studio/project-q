using System;
using Unity.Entities;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using System.Runtime.CompilerServices;
using Unity.NetCode;
using Unity.Transforms;

[assembly: InternalsVisibleTo("Higo.Camera.Editor")]
namespace Higo.Camera
{
    [Serializable]
    public struct Orbit
    {
        /// <summary>Height relative to target</summary>
        public float Height;
        /// <summary>Radius of orbit</summary>
        public float Radius;
        /// <summary>Constructor with specific values</summary>
        /// <param name="h">Orbit height</param>
        /// <param name="r">Orbit radius</param>
        public Orbit(float h, float r)
        {
            Height = h;
            Radius = r;
        }

        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case Orbit o:
                    return Height == o.Height && Radius == o.Radius;
                default:
                    return base.Equals(obj);
            }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    [DisallowMultipleComponent]
    public class CameraFreeLookAuthoringComponent : MonoBehaviour, IConvertGameObjectToEntity
    {
        public bool AutoActivate;
        public int Priority;
        public Transform Follow;
        public Transform LookAt;
        [Range(0, 1)]
        public float SplineCurvature = 0.2f;
        public Orbit TopRig = new Orbit(4.5f, 1.75f);
        public Orbit MiddleRig = new Orbit(2.5f, 3f);
        public Orbit BottomRig = new Orbit(0.4f, 1.3f);
        public CameraAxis XAxis = new CameraAxis()
        {
            Value = 0.5f,
            MinValue = -180,
            MaxValue = 180,
            AccelTime = 0.1f,
            DeaccelTime = 0.1f,
            MaxSpeed = 300f,
            Wrap = true,
        };
        public CameraAxis YAxis = new CameraAxis()
        {
            Value = 0.5f,
            MinValue = 0,
            MaxValue = 1,
            AccelTime = 0.2f,
            DeaccelTime = 0.1f,
            MaxSpeed = 2f,
            Wrap = false,
            Inverse = true,
        };

        internal float4[] cachedKnots;
        internal float4[] cachedCtrl1;
        internal float4[] cachedCtrl2;
        internal Orbit[] cacheOrbits;
        internal float cachedTension;
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            CameraUtility.UpdateOrbits(new Orbit[]
            {
                TopRig, MiddleRig, BottomRig
            }, SplineCurvature, out var knots, out var ctrl1, out var ctrl2);

            dstManager.AddComponentData(entity, new CameraFreeLookParamComponent()
            {
                SplineCurvature = SplineCurvature,
                XAxis = XAxis,
                YAxis = YAxis
            });

            var cacheds = dstManager.AddBuffer<CameraFreeLookCachedBuffer>(entity);
            cacheds.ResizeUninitialized(5);
            for (var i = 0; i < 5; i++)
            {
                cacheds[i] = new CameraFreeLookCachedBuffer()
                {
                    knot = knots[i],
                    ctrl1 = ctrl1[i],
                    ctrl2 = ctrl2[i],
                };
            }

            dstManager.AddComponentData(entity, new VirtualCameraComponent()
            {
                Priority = Priority,
                Follow = conversionSystem.GetPrimaryEntity(Follow),
                LookAt = conversionSystem.GetPrimaryEntity(LookAt),
            });

            if (AutoActivate)
            {
                dstManager.AddComponent<CameraActiveRequest>(entity);
            }
        }

        internal void UpdateCached()
        {
            var orbits = new Orbit[] { TopRig, MiddleRig, BottomRig };
            bool cacheIsValid = (cacheOrbits != null && cacheOrbits.Length == 3
                && cachedTension == SplineCurvature);
            for (int i = 0; i < 3 && cacheIsValid; ++i)
            {
                cacheIsValid = cacheOrbits[i].Equals(orbits[i]);
            }
            if (cacheIsValid) return;
            cachedTension = SplineCurvature;
            cacheOrbits = orbits;
            CameraUtility.UpdateOrbits(orbits, SplineCurvature, out cachedKnots, out cachedCtrl1, out cachedCtrl2);
        }
    }
}