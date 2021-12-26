using UnityEngine;
using UnityEditor.Animations;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Animation.Hybrid;

namespace Higo.Animation.Controller
{
    public enum AnimationStateType
    {
        Clip, BlendTree
    }

    [Serializable]
    public class AnimationStateAuthoring
    {
        public AnimationStateType Type;
        public AnimationClip Motion;
        public BlendTree Tree;
        public float Speed = 1;
    }

    [Serializable]
    public class AnimationLayerAuthoring
    {
        public string name = "Base Layer";
        public List<AnimationStateAuthoring> states = new List<AnimationStateAuthoring>()
        {
            new AnimationStateAuthoring()
        };
    }

    public class AnimatorAuthoringComponent : MonoBehaviour, IConvertGameObjectToEntity
    {
        public List<AnimationLayerAuthoring> Layers = new List<AnimationLayerAuthoring>()
        {
            new AnimationLayerAuthoring()
        };

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var layerBuffer = dstManager.AddBuffer<AnimationLayerBuffer>(entity);
            var clipResources = dstManager.AddBuffer<ClipResource>(entity);
            layerBuffer.EnsureCapacity(Layers.Count);
            foreach (var layer in Layers)
            {
                foreach (var state in layer.states)
                {
                    if (state.Type == AnimationStateType.Clip)
                    {
                        conversionSystem.DeclareAssetDependency(gameObject, state.Motion);
                        clipResources.Add(new ClipResource()
                        {
                            MotionSpeed = state.Speed,
                            Motion = conversionSystem.BlobAssetStore.GetClip(state.Motion)
                        });
                    }
                    else if (state.Type == AnimationStateType.BlendTree)
                    {
                        conversionSystem.DeclareAssetDependency(gameObject, state.Tree);
                        BlendTreeConversion.Convert(state.Tree, entity, dstManager);
                    }
                }
                layerBuffer.Add(new AnimationLayerBuffer());
            }
        }
    }
}
