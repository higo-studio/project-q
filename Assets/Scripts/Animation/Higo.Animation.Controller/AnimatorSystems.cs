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
                DestroyGraph(e, m_AnimationSystem, in data);
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
                DestroyGraph(e, m_AnimationSystem, in data);
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
            data.DeltaTimeNode = graphSystem.CreateNode<ConvertDeltaTimeToFloatNode>(rootHandler);
            data.FloatRcpNode = graphSystem.CreateNode<FloatRcpNode>(rootHandler);
            data.TimeCounterNode = graphSystem.CreateNode<TimeCounterNode>(rootHandler);
            data.TimeLoopNode = graphSystem.CreateNode<TimeLoopNode>(rootHandler);
            data.EntityNode = set.CreateComponentNode(entity);
            data.AnimatorNode = graphSystem.CreateNode<AnimatorNode>(rootHandler);

            set.SetData(data.TimeCounterNode, TimeCounterNode.KernelPorts.Speed, 1f);

            set.Connect(data.EntityNode, ComponentNode.Output<AnimationControllerLayerParamBuffer>(), data.AnimatorNode, AnimatorNode.KernelPorts.LayerBufferInput);
            set.Connect(data.EntityNode, ComponentNode.Output<AnimationControllerStateParamBuffer>(), data.AnimatorNode, AnimatorNode.KernelPorts.StateBufferInput);
            set.Connect(data.AnimatorNode, AnimatorNode.KernelPorts.Output, data.EntityNode, NodeSetAPI.ConnectionType.Feedback);

            set.Connect(data.EntityNode, data.DeltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Input);
            set.Connect(data.DeltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Output, data.TimeCounterNode, TimeCounterNode.KernelPorts.DeltaTime);
            set.Connect(data.TimeCounterNode, TimeCounterNode.KernelPorts.Time, data.TimeLoopNode, TimeLoopNode.KernelPorts.InputTime);
            set.Connect(data.TimeLoopNode, TimeLoopNode.KernelPorts.OutputTime, data.AnimatorNode, AnimatorNode.KernelPorts.Time);
            set.Connect(data.DeltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Output, data.AnimatorNode, AnimatorNode.KernelPorts.DeltaTime);

            set.SendMessage(data.TimeLoopNode, TimeLoopNode.SimulationPorts.Duration, 1.0F);
            var layerBuffer = GetBuffer<AnimationLayerResource>(entity);
            var stateBuffer = GetBuffer<AnimationStateResource>(entity);
            var b1dResource = GetBuffer<BlendTree1DResource>(entity);
            var b2dResource = GetBuffer<BlendTree2DResource>(entity);
            var clipResource = GetBuffer<ClipResource>(entity);

            var layerParamBuffer = pcb.AddBuffer<AnimationControllerLayerParamBuffer>(entity);
            layerParamBuffer.Length = layerBuffer.Length;
            var stateParamBuffer = pcb.AddBuffer<AnimationControllerStateParamBuffer>(entity);
            stateParamBuffer.Length = stateBuffer.Length;
            for (var i = 0; i < layerParamBuffer.Length; i++)
            {
                layerParamBuffer[i] = new AnimationControllerLayerParamBuffer()
                {
                    Weight = i == 0 ? 1 : 0
                };
            }

            for (var i = 0; i < stateParamBuffer.Length; i++)
            {
                stateParamBuffer[i] = new AnimationControllerStateParamBuffer()
                {
                    Weight = i == 0 ? 1 : 0,
                    ParamX = 0,
                    ParamY = 0
                };
            }
            using (var builder = new BlobBuilder(Allocator.Temp))
            {
                ref var nodeData = ref builder.ConstructRoot<AnimatorNodeData>();
                var blend1dArr = builder.Allocate(ref nodeData.blendTree1Ds, b1dResource.Length);
                var blend2dArr = builder.Allocate(ref nodeData.blendTree2DSDs, b2dResource.Length);
                var clipArr = builder.Allocate(ref nodeData.motions, clipResource.Length);
                var layers = builder.Allocate(ref nodeData.layerDatas, layerBuffer.Length);
                var states = builder.Allocate(ref nodeData.stateDatas, stateBuffer.Length);

                for (var i = 0; i < b1dResource.Length; i++)
                {
                    blend1dArr[i] = BlendTreeBuilder.CreateBlendTree1DFromComponents(b1dResource[i], entityManager, entity);
                }
                for (var i = 0; i < b2dResource.Length; i++)
                {
                    blend2dArr[i] = BlendTreeBuilder.CreateBlendTree2DFromComponents(b2dResource[i], entityManager, entity);
                }
                for (var i = 0; i < clipResource.Length; i++)
                {
                    clipArr[i] = clipResource[i].Motion;
                }
                for (var i = 0; i < layerBuffer.Length; i++)
                {
                    layers[i] = layerBuffer[i];
                }
                for (var i = 0; i < stateBuffer.Length; i++)
                {
                    states[i] = stateBuffer[i];
                }
                set.SendMessage(
                    data.AnimatorNode, AnimatorNode.SimulationPorts.NodeData,
                    builder.CreateBlobAssetReference<AnimatorNodeData>(Allocator.Persistent));
            }
            set.SendMessage(data.AnimatorNode, AnimatorNode.SimulationPorts.Rig, in rig);
            return data;
        }

        [BurstCompile]
        protected void DestroyGraph(Entity entity, IAnimationGraphSystem graphSystem,
            in AnimationControllerSystemStateGraphData data)
        {
            var set = graphSystem.Set;
            set.Destroy(data.DeltaTimeNode);
            set.Destroy(data.TimeCounterNode);
            set.Destroy(data.TimeLoopNode);
            set.Destroy(data.FloatRcpNode);
            //set.Destroy(data.EntityNode);
            set.Destroy(data.AnimatorNode);
        }
    }
}
