using Higo.Animation;
using Unity.Entities;
using Unity.Collections;
using Unity.Animation;
using Unity.DataFlowGraph;
using Unity.Burst;

namespace Higo.Animation.Controller
{
    public class AnimatorGraphSystem : SystemBase
    {
        protected IAnimationGraphSystem m_AnimationSystem;
        protected EntityCommandBuffer m_pcb;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_AnimationSystem = World.GetOrCreateSystem<ProcessDefaultAnimationGraph>();
            m_AnimationSystem.AddRef();
            m_AnimationSystem.Set.RendererModel = NodeSet.RenderExecutionModel.Islands;

            m_pcb = new EntityCommandBuffer(Allocator.Persistent, PlaybackPolicy.MultiPlayback);
        }

        protected override void OnDestroy()
        {
            if (m_AnimationSystem == null)
                return;

            var cmdBuffer = new EntityCommandBuffer(Allocator.Temp);
            Entities.ForEach((Entity e, ref AnimationControllerSystemStateGraphData data, in DynamicBuffer<AnimationStateResource> stateBuffer, in DynamicBuffer<AnimationLayerResource> layerBuffer) =>
            {
                DestroyGraph(e, m_AnimationSystem, in data, in layerBuffer, in stateBuffer);
                cmdBuffer.RemoveComponent<AnimationControllerSystemStateGraphData>(e);
            }).WithoutBurst().Run();

            cmdBuffer.Playback(EntityManager);
            cmdBuffer.Dispose();


            m_pcb.Dispose();
            base.OnDestroy();
            m_AnimationSystem.RemoveRef();
        }
        protected override void OnUpdate()
        {
            var pcb = m_pcb;
            Entities
                .WithNone<AnimationControllerSystemStateGraphData>()
                .WithAll<AnimationStateResource, AnimationLayerResource>()
                .WithAll<BlendTree1DResource, BlendTree2DResource>()
                .WithAll<BlendTree1DMotionData, BlendTree2DMotionData>()
                .WithAll<ClipResource>()
                .ForEach((
                    Entity e, ref Rig rig
                    ) =>
            {
                var data = CreateGraph(e, EntityManager, in rig, m_AnimationSystem, in pcb);
                pcb.AddComponent(e, data);
            }).WithoutBurst().WithStructuralChanges().Run();

            Entities
                .WithNone<BlendTree1DMotionData, BlendTree2DMotionData>()
                .ForEach((Entity e,
                    in AnimationControllerSystemStateGraphData data,
                    in DynamicBuffer<AnimationStateResource> stateBuffer,
                    in DynamicBuffer<AnimationLayerResource> layerBuffer
                ) =>
            {
                DestroyGraph(e, m_AnimationSystem, in data, in layerBuffer, in stateBuffer);
                pcb.RemoveComponent<AnimationControllerSystemStateGraphData>(e);
            }).WithoutBurst().Run();

            if (pcb.ShouldPlayback)
            {
                pcb.Playback(EntityManager);
            }
        }

        [BurstCompile]
        protected AnimationControllerSystemStateGraphData CreateGraph(Entity entity, EntityManager entityManager, in Rig rig, IAnimationGraphSystem graphSystem, in EntityCommandBuffer pcb)
        {
            var set = graphSystem.Set;
            var data = default(AnimationControllerSystemStateGraphData);
            var rootHandler = graphSystem.CreateGraph();
            data.ParamNode = graphSystem.CreateNode<ExtractAnimatorParametersNode>(rootHandler);
            data.DeltaTimeNode = graphSystem.CreateNode<ConvertDeltaTimeToFloatNode>(rootHandler);
            data.FloatRcpNode = graphSystem.CreateNode<FloatRcpNode>(rootHandler);
            data.TimeCounterNode = graphSystem.CreateNode<TimeCounterNode>(rootHandler);
            data.TimeLoopNode = graphSystem.CreateNode<TimeLoopNode>(rootHandler);
            data.LayerMixerNode = graphSystem.CreateNode<LayerMixerNode>(rootHandler);

            data.EntityNode = set.CreateComponentNode(entity);

            var layerBuffer = GetBuffer<AnimationLayerResource>(entity);
            var stateBuffer = GetBuffer<AnimationStateResource>(entity);
            var b1dResource = GetBuffer<BlendTree1DResource>(entity);
            var b2dResource = GetBuffer<BlendTree2DResource>(entity);
            var clipResource = GetBuffer<ClipResource>(entity);

            var layerParamBuffer = pcb.AddBuffer<AnimationControllerLayerParamBuffer>(entity);
            var stateParamBuffer = pcb.AddBuffer<AnimationControllerStateParamBuffer>(entity);


            set.SetData(data.TimeCounterNode, TimeCounterNode.KernelPorts.Speed, 1f);
            set.SetPortArraySize(data.LayerMixerNode, LayerMixerNode.KernelPorts.Inputs, layerBuffer.Length);
            set.SetPortArraySize(data.LayerMixerNode, LayerMixerNode.KernelPorts.BlendingModes, layerBuffer.Length);
            set.SetPortArraySize(data.LayerMixerNode, LayerMixerNode.KernelPorts.WeightMasks, layerBuffer.Length);
            set.SetPortArraySize(data.LayerMixerNode, LayerMixerNode.KernelPorts.Weights, layerBuffer.Length);
            set.Connect(data.LayerMixerNode, LayerMixerNode.KernelPorts.Output, data.EntityNode, NodeSetAPI.ConnectionType.Feedback);
            set.SendMessage(data.LayerMixerNode, LayerMixerNode.SimulationPorts.Rig, in rig);
            set.SendMessage(data.LayerMixerNode, LayerMixerNode.SimulationPorts.LayerCount, (ushort)layerBuffer.Length);

            set.SetPortArraySize(data.ParamNode, ExtractAnimatorParametersNode.KernelPorts.LayerWeightsOutput, layerBuffer.Length);
            set.SetPortArraySize(data.ParamNode, ExtractAnimatorParametersNode.KernelPorts.StateWeightsOutput, stateBuffer.Length);
            set.SetPortArraySize(data.ParamNode, ExtractAnimatorParametersNode.KernelPorts.StateParamXsOutput, stateBuffer.Length);
            set.SetPortArraySize(data.ParamNode, ExtractAnimatorParametersNode.KernelPorts.StateParamYsOutput, stateBuffer.Length);
            set.Connect(data.EntityNode, ComponentNode.Output<AnimationControllerLayerParamBuffer>(), data.ParamNode, ExtractAnimatorParametersNode.KernelPorts.LayerBufferInput);
            set.Connect(data.EntityNode, ComponentNode.Output<AnimationControllerStateParamBuffer>(), data.ParamNode, ExtractAnimatorParametersNode.KernelPorts.StateBufferInput);

            set.Connect(data.EntityNode, data.DeltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Input);
            set.Connect(data.DeltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Output, data.TimeCounterNode, TimeCounterNode.KernelPorts.DeltaTime);
            set.Connect(data.TimeCounterNode, TimeCounterNode.KernelPorts.Time, data.TimeLoopNode, TimeLoopNode.KernelPorts.InputTime);

            for (var layerIndex = 0; layerIndex < layerBuffer.Length; layerIndex++)
            {
                ref var layer = ref layerBuffer.ElementAt(layerIndex);
                var nmixer = graphSystem.CreateNode<NMixerNode>(rootHandler);
                set.SetPortArraySize(nmixer, NMixerNode.KernelPorts.Inputs, layer.StateCount);
                set.SetPortArraySize(nmixer, NMixerNode.KernelPorts.Weights, layer.StateCount);
                set.SetData(data.LayerMixerNode, LayerMixerNode.KernelPorts.BlendingModes, layerIndex, BlendingMode.Override);
                set.Connect(nmixer, NMixerNode.KernelPorts.Output, data.LayerMixerNode, LayerMixerNode.KernelPorts.Inputs, layerIndex);
                set.Connect(data.ParamNode, ExtractAnimatorParametersNode.KernelPorts.LayerWeightsOutput, layerIndex, data.LayerMixerNode, LayerMixerNode.KernelPorts.Weights, layerIndex);
                set.SendMessage(nmixer, NMixerNode.SimulationPorts.Rig, in rig);
                layer.NMixerNode = nmixer;

                if (layer.ChannelWeightTableCount > 0)
                {
                    var weightMasker = graphSystem.CreateNode<WeightBuilderNode>(rootHandler);
                    ref var weightEntrys = ref layer.ChannelWeightTableRef.Value.Weights;
                    set.SetPortArraySize(weightMasker, WeightBuilderNode.KernelPorts.ChannelIndices, weightEntrys.Length);
                    set.SetPortArraySize(weightMasker, WeightBuilderNode.KernelPorts.ChannelWeights, weightEntrys.Length);
                    set.SetData(weightMasker, WeightBuilderNode.KernelPorts.DefaultWeight, 1f);
                    set.SendMessage(weightMasker, WeightBuilderNode.SimulationPorts.Rig, in rig);
                    for (var i = 0; i < weightEntrys.Length; i++)
                    {
                        set.SetData(weightMasker, WeightBuilderNode.KernelPorts.ChannelIndices, i, weightEntrys[i].Index);
                        set.SetData(weightMasker, WeightBuilderNode.KernelPorts.ChannelWeights, i, weightEntrys[i].Weight);
                    }
                    set.Connect(weightMasker, WeightBuilderNode.KernelPorts.Output, data.LayerMixerNode, LayerMixerNode.KernelPorts.WeightMasks, layerIndex);
                    layer.WeightMaskNode = weightMasker;
                }

                layerParamBuffer.Add(new AnimationControllerLayerParamBuffer()
                {
                    Weight = 1f / layerBuffer.Length
                });
                for (var i = 0; i < layer.StateCount; i++)
                {
                    ref var state = ref stateBuffer.ElementAt(i + layer.StateStartIndex);
                    if (state.Type == AnimationStateType.Clip)
                    {
                        var clipNode = graphSystem.CreateNode<UberClipNode>(rootHandler);
                        state.Node = clipNode;
                        set.Connect(clipNode, UberClipNode.KernelPorts.Output, nmixer, NMixerNode.KernelPorts.Inputs, i);
                        set.Connect(data.TimeLoopNode, TimeLoopNode.KernelPorts.OutputTime, clipNode, UberClipNode.KernelPorts.Time);


                        set.SendMessage(clipNode, UberClipNode.SimulationPorts.Rig, in rig);
                        set.SendMessage(clipNode, UberClipNode.SimulationPorts.Clip, clipResource[state.ResourceId].Motion);
                        set.SendMessage(clipNode, UberClipNode.SimulationPorts.Configuration, new ClipConfiguration()
                        {
                            Mask = ClipConfigurationMask.LoopTime
                        });
                    }
                    else if (state.Type == AnimationStateType.Blend1D)
                    {
                        var btNode = graphSystem.CreateNode<UpBlendTree1DNode>(rootHandler);
                        state.Node = btNode;
                        set.Connect(btNode, UpBlendTree1DNode.KernelPorts.Output, nmixer, NMixerNode.KernelPorts.Inputs, i);
                        set.Connect(data.DeltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Output, btNode, UpBlendTree1DNode.KernelPorts.DeltaTime);
                        set.Connect(data.ParamNode, ExtractAnimatorParametersNode.KernelPorts.StateParamXsOutput, i + layer.StateStartIndex, btNode, UpBlendTree1DNode.KernelPorts.BlendValue);

                        set.SendMessage(btNode, UpBlendTree1DNode.SimulationPorts.Rig, in rig);
                        set.SendMessage(btNode, UpBlendTree1DNode.SimulationPorts.BlendTree,
                            BlendTreeBuilder.CreateBlendTree1DFromComponents(b1dResource[state.ResourceId], entityManager, entity));
                    }
                    else if (state.Type == AnimationStateType.Blend2D)
                    {
                        var btNode = graphSystem.CreateNode<UpBlendTree2DNode>(rootHandler);
                        state.Node = btNode;
                        set.Connect(btNode, UpBlendTree2DNode.KernelPorts.Output, nmixer, NMixerNode.KernelPorts.Inputs, i);
                        set.Connect(data.DeltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Output, btNode, UpBlendTree2DNode.KernelPorts.DeltaTime);
                        set.Connect(data.ParamNode, ExtractAnimatorParametersNode.KernelPorts.StateParamXsOutput, i + layer.StateStartIndex, btNode, UpBlendTree2DNode.KernelPorts.BlendValueX);
                        set.Connect(data.ParamNode, ExtractAnimatorParametersNode.KernelPorts.StateParamYsOutput, i + layer.StateStartIndex, btNode, UpBlendTree2DNode.KernelPorts.BlendValueY);

                        set.SendMessage(btNode, UpBlendTree2DNode.SimulationPorts.Rig, in rig);
                        set.SendMessage(btNode, UpBlendTree2DNode.SimulationPorts.BlendTree, BlendTreeBuilder.CreateBlendTree2DFromComponents(b2dResource[state.ResourceId], entityManager, entity));
                    }
                    set.Connect(data.ParamNode, ExtractAnimatorParametersNode.KernelPorts.StateWeightsOutput, i + layer.StateStartIndex, nmixer, NMixerNode.KernelPorts.Weights, i);

                    stateParamBuffer.Add(new AnimationControllerStateParamBuffer()
                    {
                        Weight = 1f / layer.StateCount
                    });
                }
            }

            set.SendMessage(data.TimeLoopNode, TimeLoopNode.SimulationPorts.Duration, 1.0F);

            return data;
        }

        [BurstCompile]
        protected void DestroyGraph(Entity entity, IAnimationGraphSystem graphSystem,
            in AnimationControllerSystemStateGraphData data,
            in DynamicBuffer<AnimationLayerResource> layerBuffer,
            in DynamicBuffer<AnimationStateResource> stateBuffer)
        {
            var set = graphSystem.Set;
            set.Destroy(data.DeltaTimeNode);
            set.Destroy(data.TimeCounterNode);
            set.Destroy(data.TimeLoopNode);
            set.Destroy(data.FloatRcpNode);
            set.Destroy(data.EntityNode);
            set.Destroy(data.LayerMixerNode);
            set.Destroy(data.ParamNode);
            for (var layerIndex = 0; layerIndex < layerBuffer.Length; layerIndex++)
            {
                var layer = layerBuffer[layerIndex];
                set.Destroy(layer.NMixerNode);
                for (var i = 0; i < layer.StateCount; i++)
                {
                    var state = stateBuffer[i + layer.StateStartIndex];
                    set.Destroy(state.Node);
                }
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

    // public class AnimatorNode
    //     : SimulationKernelNodeDefinition<AnimatorNode.SimPorts, AnimatorNode.KernelDefs>
    // {
    //     public struct SimPorts : ISimulationPortDefinition
    //     {
    //         public MessageInput<AnimatorNode, Rig> Rig;
    //         public MessageInput<AnimatorNode, Rig> RigOut;
    //     }

    //     [Managed]
    //     internal struct Data : INodeData, IInit, IDestroy, IMsgHandler<Rig>
    //     {
    //         void IInit.Init(InitContext ctx)
    //         {
    //             throw new System.NotImplementedException();
    //         }

    //         public void Destroy(DestroyContext context)
    //         {
    //             throw new System.NotImplementedException();
    //         }

    //         public void HandleMessage(MessageContext ctx, in Rig msg)
    //         {
    //             throw new System.NotImplementedException();
    //         }
    //     }

    //     public struct KernelDefs : IKernelPortDefinition
    //     {

    //     }

    //     struct KernelData : IKernelData
    //     {}

    //     struct GraphKernel : IGraphKernel<KernelData, KernelDefs>
    //     {
    //         public void Execute(RenderContext ctx, in KernelData data, ref KernelDefs ports)
    //         {

    //         }
    //     }
    // }
}
