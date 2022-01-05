using UnityEngine;
using Unity.Animation;
using Unity.DataFlowGraph;
//using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using System.Collections.Generic;
using Unity.Mathematics;

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

            internal PortArray<MessageOutput<AnimatorNode, BlobAssetReference<Clip>>> m_outClips;
            internal PortArray<MessageOutput<AnimatorNode, BlobAssetReference<BlendTree1D>>> m_outBlendTree1Ds;
            internal PortArray<MessageOutput<AnimatorNode, BlobAssetReference<BlendTree2DSimpleDirectional>>> m_outBlendTree2Ds;
            internal MessageOutput<AnimatorNode, float> m_outOneFloat;
            internal PortArray<MessageOutput<AnimatorNode, int>> m_outIndics;
            internal PortArray<MessageOutput<AnimatorNode, int>> m_outWeights;
            internal MessageOutput<AnimatorNode, ushort> m_outLayerCount;
        }

        [Managed]
        internal struct Data : INodeData, IInit, IDestroy, IMsgHandler<Rig>, IMsgHandler<BlobAssetReference<AnimatorNodeData>>
        {
            internal BlobAssetReference<RigDefinition> m_RigDefinition;
            NodeHandle<LayerMixerNode> LayerMixerHandler;
            NodeHandle<KernelPassThroughNodeFloat> m_TimePassThrough;
            NodeHandle<KernelPassThroughNodeFloat> m_DeltaTimePassThrough;
            List<NodeHandle<NMixerNode>> NMixerHandlers;
            List<NodeHandle<WeightBuilderNode>> WeightBuilderHandlers;
            List<NodeHandle> ClipOrTreeHandlers;

            NodeHandle<ExtractAnimatorParametersNode> ParamNode;

            void IInit.Init(InitContext ctx)
            {
                var set = ctx.Set;
                var thisHandle = set.CastHandle<AnimatorNode>(ctx.Handle);
                LayerMixerHandler = set.Create<LayerMixerNode>();
                set.Connect(thisHandle, SimulationPorts.RigOut, LayerMixerHandler, LayerMixerNode.SimulationPorts.Rig);
                ctx.ForwardOutput(KernelPorts.Output, LayerMixerHandler, LayerMixerNode.KernelPorts.Output);

                m_TimePassThrough = set.Create<KernelPassThroughNodeFloat>();
                ctx.ForwardInput(KernelPorts.Time, m_TimePassThrough, KernelPassThroughNodeFloat.KernelPorts.Input);

                m_DeltaTimePassThrough = set.Create<KernelPassThroughNodeFloat>();
                ctx.ForwardInput(KernelPorts.DeltaTime, m_DeltaTimePassThrough, KernelPassThroughNodeFloat.KernelPorts.Input);

                ParamNode = set.Create<ExtractAnimatorParametersNode>();
                ctx.ForwardInput(KernelPorts.LayerBufferInput, ParamNode, ExtractAnimatorParametersNode.KernelPorts.LayerBufferInput);
                ctx.ForwardInput(KernelPorts.StateBufferInput, ParamNode, ExtractAnimatorParametersNode.KernelPorts.StateBufferInput);
            }

            public void Destroy(DestroyContext ctx)
            {
                ctx.Set.Destroy(LayerMixerHandler);
                ctx.Set.Destroy(m_TimePassThrough);
                ctx.Set.Destroy(ParamNode);
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

                set.SetPortArraySize(thisHandle, SimulationPorts.m_outBlendTree1Ds, val.blendTree1Ds.Length);
                set.SetPortArraySize(thisHandle, SimulationPorts.m_outBlendTree2Ds, val.blendTree2DSDs.Length);
                set.SetPortArraySize(thisHandle, SimulationPorts.m_outClips, val.motions.Length);

                set.Connect(thisHandle, SimulationPorts.m_outLayerCount, LayerMixerHandler, LayerMixerNode.SimulationPorts.LayerCount);
                ctx.EmitMessage(SimulationPorts.m_outLayerCount, (ushort)val.layerDatas.Length);

                set.SetPortArraySize(ParamNode, ExtractAnimatorParametersNode.KernelPorts.LayerWeightsOutput, val.layerDatas.Length);
                var stateCount = 0;
                for (var layerIdx = 0; layerIdx < val.layerDatas.Length; layerIdx++)
                {
                    stateCount += val.layerDatas[layerIdx].StateCount;
                }
                set.SetPortArraySize(ParamNode, ExtractAnimatorParametersNode.KernelPorts.StateWeightsOutput, stateCount);
                set.SetPortArraySize(ParamNode, ExtractAnimatorParametersNode.KernelPorts.StateParamXsOutput, stateCount);
                set.SetPortArraySize(ParamNode, ExtractAnimatorParametersNode.KernelPorts.StateParamYsOutput, stateCount);

                NMixerHandlers = new List<NodeHandle<NMixerNode>>(val.layerDatas.Length);
                WeightBuilderHandlers = new List<NodeHandle<WeightBuilderNode>>(val.layerDatas.Length);
                ClipOrTreeHandlers = new List<NodeHandle>(val.layerDatas.Length);
                for (var layerIdx = 0; layerIdx < val.layerDatas.Length; layerIdx++)
                {
                    ref var layer = ref val.layerDatas[layerIdx];
                    var nmixer = set.Create<NMixerNode>();
                    set.SetPortArraySize(nmixer, NMixerNode.KernelPorts.Inputs, layer.StateCount);
                    set.SetPortArraySize(nmixer, NMixerNode.KernelPorts.Weights, layer.StateCount);

                    set.Connect(thisHandle, SimulationPorts.RigOut, nmixer, NMixerNode.SimulationPorts.Rig);
                    set.Connect(
                         nmixer, NMixerNode.KernelPorts.Output,
                         LayerMixerHandler, LayerMixerNode.KernelPorts.Inputs, layerIdx);
                    NMixerHandlers.Add(nmixer);
                    set.Connect(ParamNode, ExtractAnimatorParametersNode.KernelPorts.LayerWeightsOutput, layerIdx,
                        LayerMixerHandler, LayerMixerNode.KernelPorts.Weights, layerIdx);

                    if (layer.ChannelWeightTableCount > 0)
                    {
                        var weightMasker = set.Create<WeightBuilderNode>();
                        ref var weightEntrys = ref layer.ChannelWeightTableRef.Value.Weights;
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
                    for (var stateIdx = 0; stateIdx < layer.StateCount; stateIdx++)
                    {
                        var stateIdxInBuffer = layer.StateStartIndex + stateIdx;
                        set.Connect(ParamNode, ExtractAnimatorParametersNode.KernelPorts.StateWeightsOutput, stateIdxInBuffer,
                            nmixer, NMixerNode.KernelPorts.Weights, stateIdx);
                        ref var state = ref val.stateDatas[stateIdxInBuffer];
                        NodeHandle stateHandler = default;
                        if (state.Type == AnimationStateType.Clip)
                        {
                            var handler = set.Create<UberClipNode>();
                            set.Connect(thisHandle, SimulationPorts.RigOut, handler, UberClipNode.SimulationPorts.Rig);
                            set.Connect(thisHandle, SimulationPorts.m_outClips, state.ResourceId, handler, UberClipNode.SimulationPorts.Clip);
                            set.Connect(m_TimePassThrough, KernelPassThroughNodeFloat.KernelPorts.Output, handler, UberClipNode.KernelPorts.Time);
                            set.Connect(handler, UberClipNode.KernelPorts.Output, nmixer, NMixerNode.KernelPorts.Inputs, stateIdx);

                            stateHandler = handler;
                            ctx.EmitMessage(SimulationPorts.m_outClips, state.ResourceId, val.motions[state.ResourceId]);
                        }
                        else if (state.Type == AnimationStateType.Blend1D)
                        {
                            var handler = set.Create<UpBlendTree1DNode>();
                            set.Connect(thisHandle, SimulationPorts.RigOut, handler, UpBlendTree1DNode.SimulationPorts.Rig);
                            set.Connect(thisHandle, SimulationPorts.m_outBlendTree1Ds, state.ResourceId, handler, UpBlendTree1DNode.SimulationPorts.BlendTree);
                            set.Connect(m_DeltaTimePassThrough, KernelPassThroughNodeFloat.KernelPorts.Output, handler, UpBlendTree1DNode.KernelPorts.DeltaTime);
                            set.Connect(handler, UpBlendTree1DNode.KernelPorts.Output, nmixer, NMixerNode.KernelPorts.Inputs, stateIdx);
                            set.Connect(ParamNode, ExtractAnimatorParametersNode.KernelPorts.StateParamXsOutput, stateIdxInBuffer,
                                handler, UpBlendTree1DNode.KernelPorts.BlendValue);

                            stateHandler = handler;
                            ctx.EmitMessage(SimulationPorts.m_outBlendTree1Ds, state.ResourceId, val.blendTree1Ds[state.ResourceId]);
                        }
                        else if (state.Type == AnimationStateType.Blend2D)
                        {
                            var handler = set.Create<UpBlendTree2DNode>();
                            set.Connect(thisHandle, SimulationPorts.RigOut, handler, UpBlendTree2DNode.SimulationPorts.Rig);
                            set.Connect(thisHandle, SimulationPorts.m_outBlendTree2Ds, state.ResourceId, handler, UpBlendTree2DNode.SimulationPorts.BlendTree);
                            set.Connect(m_DeltaTimePassThrough, KernelPassThroughNodeFloat.KernelPorts.Output, handler, UpBlendTree2DNode.KernelPorts.DeltaTime);
                            set.Connect(handler, UpBlendTree2DNode.KernelPorts.Output, nmixer, NMixerNode.KernelPorts.Inputs, stateIdx);
                            set.Connect(ParamNode, ExtractAnimatorParametersNode.KernelPorts.StateParamXsOutput, stateIdxInBuffer,
                                handler, UpBlendTree2DNode.KernelPorts.BlendValueX);
                            set.Connect(ParamNode, ExtractAnimatorParametersNode.KernelPorts.StateParamYsOutput, stateIdxInBuffer,
                                handler, UpBlendTree2DNode.KernelPorts.BlendValueY);

                            stateHandler = handler;
                            ctx.EmitMessage(SimulationPorts.m_outBlendTree2Ds, state.ResourceId, val.blendTree2DSDs[state.ResourceId]);
                        }
                        ClipOrTreeHandlers.Add(stateHandler);
                    }
                }

                ctx.EmitMessage(SimulationPorts.RigOut, new Rig()
                {
                    Value = m_RigDefinition
                });

                ctx.EmitMessage(SimulationPorts.m_outOneFloat, 1f);
            }
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<AnimatorNode, float> Time;
            public DataInput<AnimatorNode, float> DeltaTime;
            public DataInput<AnimatorNode, Buffer<AnimationControllerLayerParamBuffer>> LayerBufferInput;
            public DataInput<AnimatorNode, Buffer<AnimationControllerStateParamBuffer>> StateBufferInput;

            public DataOutput<AnimatorNode, Buffer<AnimatedData>> Output;
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
            public DataInput<ExtractAnimatorParametersNode, Buffer<AnimationControllerLayerParamBuffer>> LayerBufferInput;
            public DataInput<ExtractAnimatorParametersNode, Buffer<AnimationControllerStateParamBuffer>> StateBufferInput;
            public PortArray<DataOutput<ExtractAnimatorParametersNode, float>> LayerWeightsOutput;
            public PortArray<DataOutput<ExtractAnimatorParametersNode, float>> StateWeightsOutput;
            public PortArray<DataOutput<ExtractAnimatorParametersNode, float>> StateParamXsOutput;
            public PortArray<DataOutput<ExtractAnimatorParametersNode, float>> StateParamYsOutput;
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
                var statebuffer = ctx.Resolve(ports.StateBufferInput);
                for (var i = 0; i < stateWeightArr.Length; i++)
                {
                    stateWeightArr[i] = statebuffer[i].Weight;
                    stateParamXArr[i] = statebuffer[i].ParamX;
                    stateParamYArr[i] = statebuffer[i].ParamY;
                }
            }
        }
    }
}
