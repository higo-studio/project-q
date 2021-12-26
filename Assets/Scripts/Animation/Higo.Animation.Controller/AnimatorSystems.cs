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

            m_pcb = new EntityCommandBuffer(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            if (m_AnimationSystem == null)
                return;

            var cmdBuffer = new EntityCommandBuffer(Allocator.Temp);
            Entities.ForEach((Entity e, ref AnimationControllerSystemStateData data, in DynamicBuffer<AnimationStateBuffer> stateBuffer) =>
            {
                DestroyGraph(e, m_AnimationSystem, in data, in stateBuffer);
                cmdBuffer.RemoveComponent<AnimationControllerSystemStateData>(e);
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
            Entities.WithNone<AnimationControllerSystemStateData>().ForEach((Entity e, ref DynamicBuffer<AnimationStateBuffer> stateBuffer, ref Rig rig, in DynamicBuffer<AnimationLayerBuffer> layerBuffer) =>
            {
                var data = CreateGraph(e, ref rig, m_AnimationSystem);
                pcb.AddComponent(e, data);
            }).WithoutBurst().Run();

            Entities.WithNone<AnimationLayerBuffer>().ForEach((Entity e, in AnimationControllerSystemStateData layerBuffer) =>
            {
                pcb.RemoveComponent<AnimationControllerSystemStateData>(e);
            }).WithoutBurst().Run();

            if (pcb.ShouldPlayback)
            {
                pcb.Playback(EntityManager);
            }
        }

        [BurstCompile]
        protected AnimationControllerSystemStateData CreateGraph(Entity entity, ref Rig rig, IAnimationGraphSystem graphSystem)
        {
            var data = default(AnimationControllerSystemStateData);
            var handler = graphSystem.CreateGraph();
            data.DeltaTimeNode = graphSystem.CreateNode<ConvertDeltaTimeToFloatNode>(handler);
            data.FloatRcpNode = graphSystem.CreateNode<FloatRcpNode>(handler);
            data.TimeCounterNode = graphSystem.CreateNode<TimeCounterNode>(handler);
            data.TimeLoopNode = graphSystem.CreateNode<TimeLoopNode>(handler);
            return data;
        }

        [BurstCompile]
        protected void DestroyGraph(Entity entity, IAnimationGraphSystem graphSystem,
            in AnimationControllerSystemStateData data,
            in DynamicBuffer<AnimationStateBuffer> stateBuffer,
            ref DynamicBuffer<BlendTree1DResource> tree1dBuffer,
            ref DynamicBuffer<BlendTree2DResource> tree2dBuffer,
            ref DynamicBuffer<ClipResource> clipBuffer)
        {
            var set = graphSystem.Set;
            set.Destroy(data.DeltaTimeNode);
            set.Destroy(data.TimeCounterNode);
            set.Destroy(data.TimeLoopNode);
            set.Destroy(data.FloatRcpNode);

            foreach(var state in stateBuffer)
            {
                
            }
        }
    }
}
