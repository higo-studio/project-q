using UnityEngine;
using UnityEditor.Animations;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Animation;
using Unity.Animation.Hybrid;
using System.Linq;
using Unity.Animation.Authoring;
using Unity.Collections;

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
    public struct AnimatorChannelWeightMap
    {
        public Transform Id;
        public float Weight;

        public static implicit operator AnimatorChannelWeightMap(Transform src)
        {
            return new AnimatorChannelWeightMap()
            {
                Id = src,
                Weight = 0,
            };
        }
    }

    [Serializable]
    public class AnimationLayerAuthoring
    {
        public string name = "Base Layer";
        public AnimatorChannelWeightMap[] channelWeightMap = Array.Empty<AnimatorChannelWeightMap>();
        public List<AnimationStateAuthoring> states = new List<AnimationStateAuthoring>()
        {
            new AnimationStateAuthoring()
        };
    }

    [RequireComponent(typeof(RigComponent))]
    public class AnimatorAuthoringComponent : MonoBehaviour, IConvertGameObjectToEntity
    {
        public List<AnimationLayerAuthoring> Layers = new List<AnimationLayerAuthoring>()
        {
            new AnimationLayerAuthoring()
        };

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddBuffer<AnimatorClipResource>(entity);
            dstManager.AddBuffer<AnimatorRigMaskResource>(entity);

            var rig = dstManager.GetComponentData<Rig>(entity);
            var hasher = BindingHashGlobals.DefaultHashGenerator;

            using (var builder = new BlobBuilder(Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<AnimatorNodeDataRaw>();
                var layerDataArr = builder.Allocate(ref root.layerDatas, Layers.Count);
                var stateIdGenerator = 0;
                for (var layerIndex = 0; layerIndex < Layers.Count; layerIndex++)
                {
                    var maskBuffer = dstManager.GetBuffer<AnimatorRigMaskResource>(entity);
                    var maskResIdx = -1;
                    var layer = Layers[layerIndex];
                    var animatorChannelMap = layer.channelWeightMap;
                    if (animatorChannelMap != null && animatorChannelMap.Length > 0)
                    {
                        var query = new ChannelWeightQuery();
                        var channelWeightMap = new ChannelWeightMap[animatorChannelMap.Length];
                        for (var i = 0; i < channelWeightMap.Length; i++)
                        {
                            var data = animatorChannelMap[i];
                            channelWeightMap[i] = new ChannelWeightMap()
                            {
                                Id = hasher.ToHash(ToTransformBindingID(data.Id, transform)),
                                Weight = data.Weight
                            };
                        }

                        maskResIdx = maskBuffer.Add(new AnimatorRigMaskResource()
                        {
                            ValueRef = query.ToChannelWeightTable(rig),
                        });
                    }

                    ref var layerData = ref layerDataArr[layerIndex];
                    layerData.ResourceId = maskResIdx;
                    var stateDataArr = builder.Allocate(ref layerData.stateDatas, layer.states.Count);

                    for (var stateIdx = 0; stateIdx < layer.states.Count; stateIdx++)
                    {
                        var state = layer.states[stateIdx];
                        StringHash hash = default;
                        int ResourceId = -1;
                        AnimatorStateType type = default;
                        if (state.Type == AnimationStateAuthoringType.Clip)
                        {
                            var clipBuffer = dstManager.GetBuffer<AnimatorClipResource>(entity);
                            conversionSystem.DeclareAssetDependency(gameObject, state.Motion);
                            ResourceId = clipBuffer.Add(new AnimatorClipResource()
                            {
                                MotionSpeed = state.Speed,
                                ValueRef = conversionSystem.BlobAssetStore.GetClip(state.Motion)
                            });
                            type = AnimatorStateType.Clip;
                            hash = new StringHash(state.Motion.name);
                        }
                        else if (state.Type == AnimationStateAuthoringType.BlendTree)
                        {
                            conversionSystem.DeclareAssetDependency(gameObject, state.Tree);
                            var clipConfiguration = new ClipConfiguration { Mask = ClipConfigurationMask.LoopValues };
                            if (state.Tree.blendType == BlendTreeType.Simple1D)
                            {
                                var bakeOptions = new BakeOptions { RigDefinition = rig.Value, ClipConfiguration = clipConfiguration, SampleRate = 60.0f };
                                ResourceId = BlendTreeConversion.Convert(state.Tree, entity, dstManager, bakeOptions);
                            }
                            else
                            {
                                var bakeOptions = new BakeOptions { RigDefinition = rig.Value, SampleRate = 60.0f };
                                ResourceId = BlendTreeConversion.Convert(state.Tree, entity, dstManager, bakeOptions);
                            }

                            type = state.Tree.blendType == BlendTreeType.Simple1D ? AnimatorStateType.Blend1D : AnimatorStateType.Blend2D;
                            hash = new StringHash(state.Tree.name);
                        }
                        stateDataArr[stateIdx] = new AnimatorStateDataRaw()
                        {
                            Hash = hash,
                            ResourceId = ResourceId,
                            Type = type,
                            IdInBuffer = stateIdGenerator++
                        };
                    }
                }

                root.totalStateCount = stateIdGenerator;

                dstManager.AddComponentData(entity, new AnimatorSetup()
                {
                    ValueRef = builder.CreateBlobAssetReference<AnimatorNodeDataRaw>(Allocator.Persistent)
                });
            }
        }
        static internal TransformBindingID ToTransformBindingID(Transform target, Transform root) =>
            new TransformBindingID { Path = RigGenerator.ComputeRelativePath(target, root) };

        static internal GenericBindingID ToGenericBindingID(string id) =>
            new GenericBindingID { Path = string.IsNullOrEmpty(id) ? string.Empty : System.IO.Path.GetDirectoryName(id), AttributeName = System.IO.Path.GetFileName(id) };
    }
}
