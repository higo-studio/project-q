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

    public class AnimatorInput : MonoBehaviour
    {
        public List<float> LayerWeight = new List<float>();
        public List<float> StateWeight = new List<float>();
        public Data2DInput data2d;
        private void Update()
        {
            foreach (var w in World.All)
            {
                var sys = w.GetExistingSystem<AnimatorDebugInputSystem>();
                if (sys == null) continue;
                sys.LayerWeight.Clear();
                sys.LayerWeight.AddRange(LayerWeight);
                sys.StateWeight.Clear();
                sys.StateWeight.AddRange(StateWeight);
                sys.data2d = data2d;
            }
        }
    }

    public class AnimatorDebugInputSystem : SystemBase
    {
        public List<float> LayerWeight = new List<float>();
        public List<float> StateWeight = new List<float>();
        public Data2DInput data2d;
        protected override void OnUpdate()
        {
            Entities.ForEach((ref DynamicBuffer<AnimationControllerLayerParamBuffer> layerParamBuffer, ref DynamicBuffer<AnimationControllerStateParamBuffer> stateParamBuffer) =>
            {
                var layerMinCount = math.min(layerParamBuffer.Length, LayerWeight.Count);
                for (var i = 0; i < layerMinCount; i++)
                {
                    ref var param = ref layerParamBuffer.ElementAt(i);
                    param.Weight = LayerWeight[i];
                }

                var stateMinCount = math.min(stateParamBuffer.Length, StateWeight.Count);
                for (var i = 0; i < stateMinCount; i++)
                {
                    ref var param = ref stateParamBuffer.ElementAt(i);
                    param.Weight = StateWeight[i];
                    param.ParamX = data2d.x;
                    param.ParamY = data2d.y;

                }
            }).WithoutBurst().Run();
        }
    }

}