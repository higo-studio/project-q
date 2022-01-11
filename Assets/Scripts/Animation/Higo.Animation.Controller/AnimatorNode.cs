using UnityEngine;
using Unity.Animation;
using Unity.DataFlowGraph;
//using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using System.Collections.Generic;
using Unity.Mathematics;
using System.Diagnostics;

namespace Higo.Animation.Controller
{
    public struct AnimationNodeData
    {

    }

    public class AnimatorNode
        : SimulationKernelNodeDefinition<AnimatorNode.SimPorts, AnimatorNode.KernelDefs>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<AnimatorNode, BlobAssetReference<AnimatorNodeData>> NodeData;
            public MessageInput<AnimatorNode, Rig> Rig;
            public MessageOutput<AnimatorNode, Rig> RigOut;
            public MessageOutput<AnimatorNode, BlobAssetReference<Clip>> FirstClip;
            internal MessageOutput<AnimatorNode, float> m_outOneFloat;
            internal PortArray<MessageOutput<AnimatorNode, int>> m_outIndics;
            internal PortArray<MessageOutput<AnimatorNode, int>> m_outWeights;
            internal MessageOutput<AnimatorNode, ushort> m_outLayerCount;
        }

        [Managed]
        internal struct Data : INodeData, IInit, IDestroy, IMsgHandler<Rig>,
            IMsgHandler<BlobAssetReference<AnimatorNodeData>>, IMsgHandler<BlobAssetReference<Clip>>
        {
            internal BlobAssetReference<RigDefinition> m_RigDefinition;
            NodeHandle<LayerMixerNode> LayerMixerHandler;
            NodeHandle<KernelPassThroughArrayFloat> SpeedPassThrough;
            List<NodeHandle<NMixerNode>> NMixerHandlers;
            List<NodeHandle<WeightBuilderNode>> WeightBuilderHandlers;
            List<NodeHandle> ClipOrTreeHandlers;

            NodeHandle<ExtractAnimatorParametersNode> ParamNode;

            void IInit.Init(InitContext ctx)
            {
                var set = ctx.Set;
                var thisHandle = set.CastHandle<AnimatorNode>(ctx.Handle);
                LayerMixerHandler = set.Create<LayerMixerNode>();
                SpeedPassThrough = set.Create<KernelPassThroughArrayFloat>();
                set.Connect(thisHandle, SimulationPorts.RigOut, LayerMixerHandler, LayerMixerNode.SimulationPorts.Rig);
                ctx.ForwardOutput(KernelPorts.Output, LayerMixerHandler, LayerMixerNode.KernelPorts.Output);
                ctx.ForwardOutput(KernelPorts.SpeedBufferOutput, SpeedPassThrough, KernelPassThroughArrayFloat.KernelPorts.Output);

                ParamNode = set.Create<ExtractAnimatorParametersNode>();
                ctx.ForwardInput(KernelPorts.LayerBufferInput, ParamNode, ExtractAnimatorParametersNode.KernelPorts.LayerBufferInput);
                ctx.ForwardInput(KernelPorts.StateBufferInput, ParamNode, ExtractAnimatorParametersNode.KernelPorts.StateBufferInput);
            }

            public void Destroy(DestroyContext ctx)
            {
                ctx.Set.Destroy(LayerMixerHandler);
                ctx.Set.Destroy(ParamNode);
                ctx.Set.Destroy(SpeedPassThrough);
                foreach (var handler in NMixerHandlers)
                {
                    ctx.Set.Destroy(handler);
                }
                foreach (var handler in ClipOrTreeHandlers)
                {
                    ctx.Set.Destroy(handler);
                }
                foreach (var handler in WeightBuilderHandlers)
                {
                    if (ctx.Set.Exists(handler))
                    {
                        ctx.Set.Destroy(handler);
                    }
                }
            }

            public void HandleMessage(MessageContext ctx, in Rig msg)
            {
                m_RigDefinition = msg;
                ctx.EmitMessage(SimulationPorts.RigOut, new Rig()
                {
                    Value = m_RigDefinition
                });
            }

            public void HandleMessage(MessageContext ctx, in BlobAssetReference<AnimatorNodeData> msg)
            {
                ref var val = ref msg.Value;
                var set = ctx.Set;
                var thisHandle = set.CastHandle<AnimatorNode>(ctx.Handle);

                set.Connect(thisHandle, SimulationPorts.m_outLayerCount, LayerMixerHandler, LayerMixerNode.SimulationPorts.LayerCount);
                ctx.EmitMessage(SimulationPorts.m_outLayerCount, (ushort)val.layerDatas.Length);

                set.SetPortArraySize(ParamNode, ExtractAnimatorParametersNode.KernelPorts.LayerWeightsOutput, val.layerDatas.Length);
                var totalStateCount = val.totalStateCount;
                set.SetPortArraySize(ParamNode, ExtractAnimatorParametersNode.KernelPorts.StateWeightsOutput, totalStateCount);
                set.SetPortArraySize(ParamNode, ExtractAnimatorParametersNode.KernelPorts.StateParamXsOutput, totalStateCount);
                set.SetPortArraySize(ParamNode, ExtractAnimatorParametersNode.KernelPorts.StateParamYsOutput, totalStateCount);
                set.SetPortArraySize(ParamNode, ExtractAnimatorParametersNode.KernelPorts.StateTimesOutput, totalStateCount);
                set.SetPortArraySize(SpeedPassThrough, KernelPassThroughArrayFloat.KernelPorts.Input, totalStateCount);
                set.SetPortArraySize(SpeedPassThrough, KernelPassThroughArrayFloat.KernelPorts.Output, totalStateCount);

                NMixerHandlers = new List<NodeHandle<NMixerNode>>(val.layerDatas.Length);
                WeightBuilderHandlers = new List<NodeHandle<WeightBuilderNode>>(val.layerDatas.Length);
                ClipOrTreeHandlers = new List<NodeHandle>(val.layerDatas.Length);

                var stateBufferStartIdx = 0;
                for (var layerIdx = 0; layerIdx < val.layerDatas.Length; layerIdx++)
                {
                    ref var layer = ref val.layerDatas[layerIdx];
                    var stateCount = layer.stateDatas.Length;
                    var nmixer = set.Create<NMixerNode>();
                    set.SetPortArraySize(nmixer, NMixerNode.KernelPorts.Inputs, stateCount);
                    set.SetPortArraySize(nmixer, NMixerNode.KernelPorts.Weights, stateCount);

                    set.Connect(thisHandle, SimulationPorts.RigOut, nmixer, NMixerNode.SimulationPorts.Rig);
                    set.Connect(
                         nmixer, NMixerNode.KernelPorts.Output,
                         LayerMixerHandler, LayerMixerNode.KernelPorts.Inputs, layerIdx);
                    NMixerHandlers.Add(nmixer);
                    set.Connect(ParamNode, ExtractAnimatorParametersNode.KernelPorts.LayerWeightsOutput, layerIdx,
                        LayerMixerHandler, LayerMixerNode.KernelPorts.Weights, layerIdx);

                    BlobAssetReference<ChannelWeightTable> TypedWeightRef = layer.ChannelWeightTableRef;
                    if (TypedWeightRef.IsCreated)
                    {
                        var weightMasker = set.Create<WeightBuilderNode>();
                        ref var weightEntrys = ref TypedWeightRef.Value.Weights;
                        set.SetPortArraySize(weightMasker, WeightBuilderNode.KernelPorts.ChannelIndices, weightEntrys.Length);
                        set.SetPortArraySize(weightMasker, WeightBuilderNode.KernelPorts.ChannelWeights, weightEntrys.Length);
                        set.Connect(thisHandle, SimulationPorts.m_outOneFloat, weightMasker, WeightBuilderNode.KernelPorts.DefaultWeight);
                        set.Connect(thisHandle, SimulationPorts.RigOut, weightMasker, WeightBuilderNode.SimulationPorts.Rig);
                        for (var i = 0; i < weightEntrys.Length; i++)
                        {
                            set.SetData(weightMasker, WeightBuilderNode.KernelPorts.ChannelIndices, i, weightEntrys[i].Index);
                            set.SetData(weightMasker, WeightBuilderNode.KernelPorts.ChannelWeights, i, weightEntrys[i].Weight);
                        }
                        set.Connect(weightMasker, WeightBuilderNode.KernelPorts.Output, LayerMixerHandler, LayerMixerNode.KernelPorts.WeightMasks, layerIdx);
                        WeightBuilderHandlers.Add(weightMasker);
                    }
                    for (var stateIdx = 0; stateIdx < stateCount; stateIdx++)
                    {
                        var stateIdxInBuffer = stateBufferStartIdx + stateIdx;
                        set.Connect(ParamNode, ExtractAnimatorParametersNode.KernelPorts.StateWeightsOutput, stateIdxInBuffer,
                            nmixer, NMixerNode.KernelPorts.Weights, stateIdx);
                        ref var state = ref layer.stateDatas[stateIdx];
                        NodeHandle stateHandler = default;
                        if (state.Type == AnimatorStateType.Clip)
                        {
                            var handler = set.Create<UberClipNode>();

                            set.Connect(thisHandle, SimulationPorts.RigOut, handler, UberClipNode.SimulationPorts.Rig);
                            set.Connect(handler, UberClipNode.KernelPorts.Output, nmixer, NMixerNode.KernelPorts.Inputs, stateIdx);
                            set.Connect(ParamNode, ExtractAnimatorParametersNode.KernelPorts.StateTimesOutput, stateIdxInBuffer, handler, UberClipNode.KernelPorts.Time);
                            set.SendMessage(handler, UberClipNode.SimulationPorts.Clip, state.ResourceRef);
                            set.SetData(SpeedPassThrough, KernelPassThroughArrayFloat.KernelPorts.Input, stateIdxInBuffer, 1f);
                            stateHandler = handler;

                        }
                        else if (state.Type == AnimatorStateType.Blend1D)
                        {
                            var handler = set.Create<UpBlendTree1DNode>();
                            set.Connect(thisHandle, SimulationPorts.RigOut, handler, UpBlendTree1DNode.SimulationPorts.Rig);
                            set.Connect(handler, UpBlendTree1DNode.KernelPorts.Output, nmixer, NMixerNode.KernelPorts.Inputs, stateIdx);
                            set.Connect(ParamNode, ExtractAnimatorParametersNode.KernelPorts.StateParamXsOutput, stateIdxInBuffer,
                                handler, UpBlendTree1DNode.KernelPorts.BlendValue);
                            set.Connect(ParamNode, ExtractAnimatorParametersNode.KernelPorts.StateTimesOutput, stateIdxInBuffer, handler, UpBlendTree1DNode.KernelPorts.Time);
                            set.Connect(handler, UpBlendTree1DNode.KernelPorts.OutputSpeed, SpeedPassThrough, KernelPassThroughArrayFloat.KernelPorts.Input, stateIdxInBuffer);

                            stateHandler = handler;
                            set.SendMessage(handler, UpBlendTree1DNode.SimulationPorts.BlendTree, state.ResourceRef);
                        }
                        else if (state.Type == AnimatorStateType.Blend2D)
                        {
                            var handler = set.Create<UpBlendTree2DNode>();
                            set.Connect(thisHandle, SimulationPorts.RigOut, handler, UpBlendTree2DNode.SimulationPorts.Rig);
                            set.Connect(handler, UpBlendTree2DNode.KernelPorts.Output, nmixer, NMixerNode.KernelPorts.Inputs, stateIdx);
                            set.Connect(ParamNode, ExtractAnimatorParametersNode.KernelPorts.StateParamXsOutput, stateIdxInBuffer,
                                handler, UpBlendTree2DNode.KernelPorts.BlendValueX);
                            set.Connect(ParamNode, ExtractAnimatorParametersNode.KernelPorts.StateParamYsOutput, stateIdxInBuffer,
                                handler, UpBlendTree2DNode.KernelPorts.BlendValueY);
                            set.Connect(handler, UpBlendTree2DNode.KernelPorts.OutputSpeed, SpeedPassThrough, KernelPassThroughArrayFloat.KernelPorts.Input, stateIdxInBuffer);

                            stateHandler = handler;
                            set.SendMessage(handler, UpBlendTree2DNode.SimulationPorts.BlendTree, state.ResourceRef);
                        }
                        ClipOrTreeHandlers.Add(stateHandler);
                    }
                    stateBufferStartIdx += stateCount;
                }

                ctx.EmitMessage(SimulationPorts.RigOut, new Rig()
                {
                    Value = m_RigDefinition
                });

                ctx.EmitMessage(SimulationPorts.m_outOneFloat, 1f);
            }

            public void HandleMessage(MessageContext ctx, in BlobAssetReference<Clip> msg)
            {
            }
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<AnimatorNode, Buffer<AnimatorLayerBuffer>> LayerBufferInput;
            public DataInput<AnimatorNode, Buffer<AnimatorStateBuffer>> StateBufferInput;

            public DataOutput<AnimatorNode, Buffer<AnimatedData>> Output;
            public PortArray<DataOutput<AnimatorNode, float>> SpeedBufferOutput;
        }

        struct KernelData : IKernelData
        { }

        struct GraphKernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext ctx, in KernelData data, ref KernelDefs ports)
            {

            }
        }
    }
    public class ExtractAnimatorParametersNode
        : KernelNodeDefinition<ExtractAnimatorParametersNode.KernelDefs>
    {
        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<ExtractAnimatorParametersNode, Buffer<AnimatorLayerBuffer>> LayerBufferInput;
            public DataInput<ExtractAnimatorParametersNode, Buffer<AnimatorStateBuffer>> StateBufferInput;
            public PortArray<DataOutput<ExtractAnimatorParametersNode, float>> LayerWeightsOutput;
            public PortArray<DataOutput<ExtractAnimatorParametersNode, float>> StateWeightsOutput;
            public PortArray<DataOutput<ExtractAnimatorParametersNode, float>> StateParamXsOutput;
            public PortArray<DataOutput<ExtractAnimatorParametersNode, float>> StateParamYsOutput;
            public PortArray<DataOutput<ExtractAnimatorParametersNode, float>> StateTimesOutput;
        }

        struct KernelData : IKernelData
        {
        }

        [BurstCompile]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext ctx, in KernelData data, ref KernelDefs ports)
            {
                var layerWeightArr = ctx.Resolve(ref ports.LayerWeightsOutput);
                var layerbuffer = ctx.Resolve(ports.LayerBufferInput);

                for (var i = 0; i < layerWeightArr.Length; i++)
                {
                    layerWeightArr[i] = layerbuffer[i].Weight;
                }

                var stateWeightArr = ctx.Resolve(ref ports.StateWeightsOutput);
                var stateParamXArr = ctx.Resolve(ref ports.StateParamXsOutput);
                var stateParamYArr = ctx.Resolve(ref ports.StateParamYsOutput);
                var stateTime = ctx.Resolve(ref ports.StateTimesOutput);
                var statebuffer = ctx.Resolve(ports.StateBufferInput);
                for (var i = 0; i < stateWeightArr.Length; i++)
                {
                    stateWeightArr[i] = statebuffer[i].Weight;
                    stateParamXArr[i] = statebuffer[i].ParamX;
                    stateParamYArr[i] = statebuffer[i].ParamY;
                    stateTime[i] = statebuffer[i].Time;
                }
            }
        }
    }

    public class KernelPassThroughArrayFloat
    : KernelNodeDefinition<KernelPassThroughArrayFloat.KernelDefs>
    {
        public struct KernelDefs : IKernelPortDefinition
        {
            public PortArray<DataInput<KernelPassThroughArrayFloat, float>> Input;
            public PortArray<DataOutput<KernelPassThroughArrayFloat, float>> Output;
        }

        struct KernelData : IKernelData
        {
        }

        [BurstCompile]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext ctx, in KernelData data, ref KernelDefs ports)
            {
                var inputs = ctx.Resolve(ports.Input);
                var outputs = ctx.Resolve(ref ports.Output);

                ValidateBufferLengthsAreEqual(inputs.Length, outputs.Length);
                for (var i = 0; i < inputs.Length; i++)
                {
                    outputs[i] = inputs[i];
                }
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateBufferLengthsAreEqual(int expected, int value)
        {
            if (expected != value)
                throw new System.InvalidOperationException($"Buffer length must match: '{expected}' and '{value}'.");
        }
    }
}
