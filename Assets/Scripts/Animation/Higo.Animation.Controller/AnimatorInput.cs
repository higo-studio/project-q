using Unity.Entities;
using Unity.Entities.Hybrid;
using Unity.Animation;
using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;
using System;

namespace Higo.Animation.Controller
{
    [Serializable]
    public struct Data2DInput
    {
        [Range(-1, 1)]
        public float x;
        [Range(-1, 1)]
        public float y;
    }
}
