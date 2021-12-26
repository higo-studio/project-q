using UnityEngine;
using UnityEditor.Animations;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Animation.Hybrid;
using System.Linq;

namespace Higo.Animation.Controller
{
    public enum AnimationStateAuthoringType
    {
        Clip, BlendTree
    }

    [Serializable]
    public class AnimationStateAuthoring
    {
        public AnimationStateAuthoringType Type;
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
            {
                var stateBuffer = dstManager.AddBuffer<AnimationStateBuffer>(entity);
                stateBuffer.EnsureCapacity(Layers.Sum(l => l.states != null ? l.states.Count : 0));
                dstManager.AddBuffer<ClipResource>(entity);
                var layerBuffer = dstManager.AddBuffer<AnimationLayerBuffer>(entity);
                layerBuffer.EnsureCapacity(Layers.Count);
            }
            for (var layerIndex = 0; layerIndex < Layers.Count; layerIndex++)
            {
                var layer = Layers[layerIndex];
                var layerBuffer = dstManager.GetBuffer<AnimationLayerBuffer>(entity);
                layerBuffer.Add(new AnimationLayerBuffer()
                {
                    StateCount = layer.states.Count
                });
                foreach (var state in layer.states)
                {
                    int resourceId = default;
                    AnimationStateType type = default;
                    if (state.Type == AnimationStateAuthoringType.Clip)
                    {
                        conversionSystem.DeclareAssetDependency(gameObject, state.Motion);
                        var clipResources = dstManager.GetBuffer<ClipResource>(entity);
                        resourceId = clipResources.Add(new ClipResource()
                        {
                            MotionSpeed = state.Speed,
                            Motion = conversionSystem.BlobAssetStore.GetClip(state.Motion)
                        });
                        type = AnimationStateType.Clip;
                    }
                    else if (state.Type == AnimationStateAuthoringType.BlendTree)
                    {
                        conversionSystem.DeclareAssetDependency(gameObject, state.Tree);
                        resourceId = BlendTreeConversion.Convert(state.Tree, entity, dstManager);
                        type = state.Tree.blendType == BlendTreeType.Simple1D ? AnimationStateType.Blend1D : AnimationStateType.Blend2D;
                    }
                    var stateBuffer = dstManager.GetBuffer<AnimationStateBuffer>(entity);
                    stateBuffer.Add(new AnimationStateBuffer()
                    {
                        ResourceId = resourceId,
                        Type = type
                    });
                }
            }
        }
    }
}
