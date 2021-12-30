using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(guid: "b4535bca721444e69e7ef91c5c2cf640", version: 1, "Animation High-Level")]
    public class UpBlendTree1DNode :
        SimulationKernelNodeDefinition<UpBlendTree1DNode.MessagePorts, UpBlendTree1DNode.DataPorts>,
        IRigContextHandler<UpBlendTree1DNode.NodeData>
    {
        public struct MessagePorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "cee8ee0efe0f4630bb5402d4ee5465cf", isHidden: true)] public MessageInput<UpBlendTree1DNode, Rig> Rig;
            [PortDefinition(guid: "c64cb9cd15f6486b88a764671399aa13", "1D Blend Tree")] public MessageInput<UpBlendTree1DNode, BlobAssetReference<BlendTree1D>> BlendTree;

#pragma warning disable 649  // Assigned through internal DataFlowGraph reflection
            internal MessageOutput<UpBlendTree1DNode, float> OutInternalTimeValue;
            internal MessageOutput<UpBlendTree1DNode, float> OutInternalDurationValue;
#pragma warning restore 649
        }

        public struct DataPorts : IKernelPortDefinition
        {
            public DataInput<UpBlendTree1DNode, float> DeltaTime;
            public DataInput<UpBlendTree1DNode, float> BlendValue;
            [PortDefinition(guid: "35c4053760a942a08aac29e384ec4db4")]
            public DataOutput<UpBlendTree1DNode, Buffer<AnimatedData>> Output;

        }

        private struct NodeData : INodeData, IInit, IDestroy,
                                  IMsgHandler<Rig>,
                                  IMsgHandler<BlobAssetReference<BlendTree1D>>
        {
            public NodeHandle<Unity.Animation.BlendTree1DNode> BlendTree1DNode;
#pragma warning disable 0618 // TODO : Remove usage of the Deltatime node in our samples
            // public NodeHandle<DeltaTimeNode> DeltaTimeNode;
#pragma warning restore 0618
            public NodeHandle<TimeCounterNode> TimeCounterNode;
            public NodeHandle<TimeLoopNode> TimeLoopNode;
            public NodeHandle<FloatRcpNode> FloatRcpNode;

            public void Init(InitContext ctx)
            {
                var thisHandle = ctx.Set.CastHandle<UpBlendTree1DNode>(ctx.Handle);

                BlendTree1DNode = ctx.Set.Create<Unity.Animation.BlendTree1DNode>();
                FloatRcpNode = ctx.Set.Create<FloatRcpNode>();
                TimeCounterNode = ctx.Set.Create<TimeCounterNode>();
                TimeLoopNode = ctx.Set.Create<TimeLoopNode>();

                ctx.Set.Connect(BlendTree1DNode, Unity.Animation.BlendTree1DNode.KernelPorts.Duration, FloatRcpNode, Unity.Animation.FloatRcpNode.KernelPorts.Input);
                ctx.Set.Connect(FloatRcpNode, Unity.Animation.FloatRcpNode.KernelPorts.Output, TimeCounterNode, Unity.Animation.TimeCounterNode.KernelPorts.Speed);
                ctx.Set.Connect(TimeCounterNode, Unity.Animation.TimeCounterNode.KernelPorts.Time, TimeLoopNode, Unity.Animation.TimeLoopNode.KernelPorts.InputTime);
                ctx.Set.Connect(TimeLoopNode, Unity.Animation.TimeLoopNode.KernelPorts.NormalizedTime, BlendTree1DNode, Unity.Animation.BlendTree1DNode.KernelPorts.NormalizedTime);

                ctx.Set.Connect(thisHandle, SimulationPorts.OutInternalTimeValue, TimeCounterNode, Unity.Animation.TimeCounterNode.SimulationPorts.Time);
                ctx.Set.Connect(thisHandle, SimulationPorts.OutInternalDurationValue, TimeLoopNode, Unity.Animation.TimeLoopNode.SimulationPorts.Duration);

                ctx.ForwardInput(KernelPorts.DeltaTime, TimeCounterNode, Unity.Animation.TimeCounterNode.KernelPorts.DeltaTime);
                ctx.ForwardInput(KernelPorts.BlendValue, BlendTree1DNode, Unity.Animation.BlendTree1DNode.KernelPorts.BlendParameter);
                ctx.ForwardInput(SimulationPorts.Rig, BlendTree1DNode, Unity.Animation.BlendTree1DNode.SimulationPorts.Rig);
                ctx.ForwardInput(SimulationPorts.BlendTree, BlendTree1DNode, Unity.Animation.BlendTree1DNode.SimulationPorts.BlendTree);
                ctx.ForwardOutput(KernelPorts.Output, BlendTree1DNode, Unity.Animation.BlendTree1DNode.KernelPorts.Output);

                ctx.EmitMessage(SimulationPorts.OutInternalTimeValue, 0F);
                ctx.EmitMessage(SimulationPorts.OutInternalDurationValue, 1F);
            }

            public void Destroy(DestroyContext ctx)
            {
                ctx.Set.Destroy(BlendTree1DNode);
                ctx.Set.Destroy(FloatRcpNode);
                ctx.Set.Destroy(TimeCounterNode);
                // ctx.Set.Destroy(DeltaTimeNode);
                ctx.Set.Destroy(TimeLoopNode);
            }

            public void HandleMessage(MessageContext ctx, in Rig msg)
            {
            }

            public void HandleMessage(MessageContext ctx, in BlobAssetReference<BlendTree1D> msg)
            {
            }
        }

        public struct KernelData : IKernelData
        {
        }

        public struct Kernel : IGraphKernel<KernelData, DataPorts>
        {
            public void Execute(RenderContext ctx, in KernelData data, ref DataPorts ports)
            {
            }
        }

        public InputPortID GetPort(NodeHandle handle)
        {
            return (InputPortID)SimulationPorts.Rig;
        }
    }
}
