using UnityEngine;
using Unity.Entities;
using System;
namespace Higo.Camera
{
    [Serializable]
    public struct CameraAxis
    {
        [Range(0, 1)]
        public float Value;
        public float MinValue;
        public float MaxValue;
        public float AccelTime;
        public float DeaccelTime;
        public float MaxSpeed;
        public float CurrentSpeed;
        public bool Wrap;
    }

    public struct CameraFreeLookParamComponent : IComponentData
    {
        public float SplineCurvature;
        public CameraAxis XAxis;
        public CameraAxis YAxis;
    }
}
