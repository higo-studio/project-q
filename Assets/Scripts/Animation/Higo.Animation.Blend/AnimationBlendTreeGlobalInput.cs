using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;
using System;
using Unity.Animation;

namespace Higo.Animation
{
    [Serializable]
    public struct Data2DInput
    {
        [Range(-1, 1)]
        public float x;
        [Range(-1, 1)]
        public float y;
    }

    public class AnimationBlendTreeGlobalInput : MonoBehaviour
    {
        [Range(0, 1f)]
        public float data1d;
        public Data2DInput data2d;

        private void Update()
        {
            foreach (var w in World.All)
            {
                var sys = w.GetExistingSystem<AnimationBlendDebugInput>();
                if (sys == null) continue;
                sys.data1d = data1d;
                sys.data2d = data2d;
            }
        }
    }

    [UpdateInGroup(typeof(DefaultAnimationSystemGroup))]
    public class AnimationBlendDebugInput : SystemBase
    {
        public float data1d;
        public Data2DInput data2d;
        protected override void OnUpdate()
        {
            var d1d = data1d;
            var d2d = data2d;
            Entities.ForEach((ref BlendTree1DData blend) =>
            {
                blend.paramX = d1d;
            }).Schedule();
            Entities.ForEach((ref BlendTree2DData blend) =>
            {
                blend.paramX = d2d.x;
                blend.paramY = d2d.y;
            }).Schedule();
        }
    }
}