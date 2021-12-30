using UnityEngine;
using UnityEditor.Animations;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Animation;
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
            dstManager.AddBuffer<AnimationStateResource>(entity);
            dstManager.AddBuffer<ClipResource>(entity);
            dstManager.AddBuffer<AnimationLayerResource>(entity);
            dstManager.AddComponent<DeltaTime>(entity);
            var rig = dstManager.GetComponentData<Rig>(entity);
            for (var layerIndex = 0; layerIndex < Layers.Count; layerIndex++)
            {
                var layer = Layers[layerIndex];
                {
                    var layerBuffer = dstManager.GetBuffer<AnimationLayerResource>(entity);
                    var stateBuffer = dstManager.GetBuffer<AnimationStateResource>(entity);
                    layerBuffer.Add(new AnimationLayerResource()
                    {
                        Count = layer.states.Count,
                        StartIndex = stateBuffer.Length
                    });
                }
                foreach (var state in layer.states)
                {
                    StringHash hash = default;
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
                        hash = new StringHash(state.Motion.name);
                    }
                    else if (state.Type == AnimationStateAuthoringType.BlendTree)
                    {
                        conversionSystem.DeclareAssetDependency(gameObject, state.Tree);
                        var clipConfiguration = new ClipConfiguration { Mask = ClipConfigurationMask.LoopValues };
                        if (state.Tree.blendType == BlendTreeType.Simple1D)
                        {
                            var bakeOptions = new BakeOptions { RigDefinition = rig.Value, ClipConfiguration = clipConfiguration, SampleRate = 60.0f };
                            resourceId = BlendTreeConversion.Convert(state.Tree, entity, dstManager, bakeOptions);
                        }
                        else
                        {
                            var bakeOptions = new BakeOptions { RigDefinition = rig.Value, SampleRate = 60.0f };
                            resourceId = BlendTreeConversion.Convert(state.Tree, entity, dstManager, bakeOptions);
                        }

                        type = state.Tree.blendType == BlendTreeType.Simple1D ? AnimationStateType.Blend1D : AnimationStateType.Blend2D;
                        hash = new StringHash(state.Tree.name);
                    }
                    var stateBuffer = dstManager.GetBuffer<AnimationStateResource>(entity);
                    stateBuffer.Add(new AnimationStateResource()
                    {
                        Hash = hash,
                        ResourceId = resourceId,
                        Type = type
                    });
                }
            }
        }
    }
}
