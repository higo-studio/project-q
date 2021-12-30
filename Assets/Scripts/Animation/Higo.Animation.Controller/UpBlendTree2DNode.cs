using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(guid: "0fa3e1906f2d490493fb54df73c64fe9", version: 1, "Animation High-Level")]
    public class UpBlendTree2DNode :
        SimulationKernelNodeDefinition<UpBlendTree2DNode.MessagePorts, UpBlendTree2DNode.DataPorts>,
        IRigContextHandler<UpBlendTree2DNode.NodeData>
    {
        public struct MessagePorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "07af14d08653483099fa544656fb5393", isHidden: true)] public MessageInput<UpBlendTree2DNode, Rig> Rig;
            [PortDefinition(guid: "f5ba27381f2c4b2b9d0fb1a51bdfe456", "2D Simple Directional Blend Tree")] public MessageInput<UpBlendTree2DNode, BlobAssetReference<BlendTree2DSimpleDirectional>> BlendTree;

#pragma warning disable 649  // Assigned through internal DataFlowGraph reflection
            internal MessageOutput<UpBlendTree2DNode, float> OutInternalTimeValue;
            internal MessageOutput<UpBlendTree2DNode, float> OutInternalDurationValue;
#pragma warning restore 649
        }

        public struct DataPorts : IKernelPortDefinition
        {
            public DataInput<UpBlendTree2DNode, float> DeltaTime;
            public DataInput<UpBlendTree2DNode, float> BlendValueX;
            public DataInput<UpBlendTree2DNode, float> BlendValueY;
            [PortDefinition(guid: "d9d53f83c0a24e4c977854e400425a1c")]
            public DataOutput<UpBlendTree2DNode, Buffer<AnimatedData>> Output;
        }

        private struct NodeData : INodeData, IInit, IDestroy,
                                  IMsgHandler<Rig>,
                                  IMsgHandler<BlobAssetReference<BlendTree2DSimpleDirectional>>
        {
            public NodeHandle<Unity.Animation.BlendTree2DNode> BlendTree2DNode;
            public NodeHandle<TimeCounterNode> TimeCounterNode;
            public NodeHandle<TimeLoopNode> TimeLoopNode;
            public NodeHandle<FloatRcpNode> FloatRcpNode;

            public void Init(InitContext ctx)
            {
                var thisHandle = ctx.Set.CastHandle<UpBlendTree2DNode>(ctx.Handle);

                BlendTree2DNode = ctx.Set.Create<Unity.Animation.BlendTree2DNode>();
                FloatRcpNode = ctx.Set.Create<FloatRcpNode>();
                TimeCounterNode = ctx.Set.Create<TimeCounterNode>();
                TimeLoopNode = ctx.Set.Create<TimeLoopNode>();

                ctx.Set.Connect(BlendTree2DNode, Unity.Animation.BlendTree2DNode.KernelPorts.Duration, FloatRcpNode, Unity.Animation.FloatRcpNode.KernelPorts.Input);
                ctx.Set.Connect(FloatRcpNode, Unity.Animation.FloatRcpNode.KernelPorts.Output, TimeCounterNode, Unity.Animation.TimeCounterNode.KernelPorts.Speed);
                ctx.Set.Connect(TimeCounterNode, Unity.Animation.TimeCounterNode.KernelPorts.Time, TimeLoopNode, Unity.Animation.TimeLoopNode.KernelPorts.InputTime);
                ctx.Set.Connect(TimeLoopNode, Unity.Animation.TimeLoopNode.KernelPorts.NormalizedTime, BlendTree2DNode, Unity.Animation.BlendTree2DNode.KernelPorts.NormalizedTime);

                ctx.Set.Connect(thisHandle, SimulationPorts.OutInternalTimeValue, TimeCounterNode, Unity.Animation.TimeCounterNode.SimulationPorts.Time);
                ctx.Set.Connect(thisHandle, SimulationPorts.OutInternalDurationValue, TimeLoopNode, Unity.Animation.TimeLoopNode.SimulationPorts.Duration);

                
                ctx.ForwardInput(KernelPorts.DeltaTime, TimeCounterNode, Unity.Animation.TimeCounterNode.KernelPorts.DeltaTime);
                ctx.ForwardInput(KernelPorts.BlendValueX, BlendTree2DNode, Unity.Animation.BlendTree2DNode.KernelPorts.BlendParameterX);
                ctx.ForwardInput(KernelPorts.BlendValueY, BlendTree2DNode, Unity.Animation.BlendTree2DNode.KernelPorts.BlendParameterY);
                ctx.ForwardInput(SimulationPorts.Rig, BlendTree2DNode, Unity.Animation.BlendTree2DNode.SimulationPorts.Rig);
                ctx.ForwardInput(SimulationPorts.BlendTree, BlendTree2DNode, Unity.Animation.BlendTree2DNode.SimulationPorts.BlendTree);
                ctx.ForwardOutput(KernelPorts.Output, BlendTree2DNode, Unity.Animation.BlendTree2DNode.KernelPorts.Output);

                ctx.EmitMessage(SimulationPorts.OutInternalTimeValue, 0F);
                ctx.EmitMessage(SimulationPorts.OutInternalDurationValue, 1F);
            }

            public void Destroy(DestroyContext ctx)
            {
                ctx.Set.Destroy(BlendTree2DNode);
                ctx.Set.Destroy(FloatRcpNode);
                ctx.Set.Destroy(TimeCounterNode);
                ctx.Set.Destroy(TimeLoopNode);
            }

            public void HandleMessage(MessageContext ctx, in Rig msg)
            {
            }

            public void HandleMessage(MessageContext ctx, in BlobAssetReference<BlendTree2DSimpleDirectional> msg)
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
