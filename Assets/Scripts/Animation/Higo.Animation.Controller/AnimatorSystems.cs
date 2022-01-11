using Higo.Animation;
using Unity.Entities;
using Unity.Collections;
using Unity.Animation;
using Unity.DataFlowGraph;
using Unity.Burst;

namespace Higo.Animation.Controller
{
    [UpdateBefore(typeof(DefaultAnimationSystemGroup))]
    public class AnimatorGraphSystem : SystemBase
    {
        protected IAnimationGraphSystem m_AnimationSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_AnimationSystem = World.GetOrCreateSystem<ProcessDefaultAnimationGraph>();
            m_AnimationSystem.AddRef();
            m_AnimationSystem.Set.RendererModel = NodeSet.RenderExecutionModel.Islands;
        }

        protected override void OnDestroy()
        {
            if (m_AnimationSystem == null)
                return;

            var cmdBuffer = new EntityCommandBuffer(Allocator.Temp);
            Entities.ForEach((Entity e, ref AnimatorGraphData data) =>
            {
                DestroyGraph(e, m_AnimationSystem, in data);
                cmdBuffer.RemoveComponent<AnimatorGraphData>(e);
            }).WithStructuralChanges().WithoutBurst().Run();

            cmdBuffer.Playback(EntityManager);
            cmdBuffer.Dispose();


            base.OnDestroy();
            m_AnimationSystem.RemoveRef();
        }
        protected override void OnUpdate()
        {
            var pcb = new EntityCommandBuffer(Allocator.Temp);
            Entities
                .WithNone<AnimatorGraphData>()
                .ForEach((
                    Entity e, ref Rig rig, in AnimatorSetup setup,
                    in DynamicBuffer<AnimatorRigMaskResource> maskRes,
                    in DynamicBuffer<AnimatorClipResource> clipRes,
                    in DynamicBuffer<BlendTree1DResource> b1dRes,
                    in DynamicBuffer<BlendTree2DResource> b2dRes
                    ) =>
            {
                var data = CreateGraph(e, in setup,
                    maskRes.ToNativeArray(Allocator.Temp),
                    clipRes.ToNativeArray(Allocator.Temp),
                    b1dRes.ToNativeArray(Allocator.Temp),
                    b2dRes.ToNativeArray(Allocator.Temp),
                    in rig, m_AnimationSystem, in pcb);
                pcb.AddComponent(e, data);
            }).WithStructuralChanges().WithoutBurst().Run();

            Entities
                .WithNone<AnimatorSetup>()
                .ForEach((Entity e,
                    in AnimatorGraphData data
                ) =>
            {
                DestroyGraph(e, m_AnimationSystem, in data);
                pcb.RemoveComponent<AnimatorGraphData>(e);
            }).WithStructuralChanges().WithoutBurst().Run();

            pcb.Playback(EntityManager);
            pcb.Dispose();
        }

        [BurstCompile]
        protected AnimatorGraphData CreateGraph(Entity entity,
            in AnimatorSetup setup,
            in NativeArray<AnimatorRigMaskResource> maskRes,
            in NativeArray<AnimatorClipResource> clipRes,
            in NativeArray<BlendTree1DResource> b1dRes,
            in NativeArray<BlendTree2DResource> b2dRes,
            in Rig rig, IAnimationGraphSystem graphSystem, in EntityCommandBuffer pcb)
        {
            var set = graphSystem.Set;
            var data = default(AnimatorGraphData);
            var rootHandler = graphSystem.CreateGraph();
            data.EntityNode = set.CreateComponentNode(entity);
            data.AnimatorNode = set.Create<AnimatorNode>();

            set.Connect(data.EntityNode, ComponentNode.Output<AnimatorLayerBuffer>(), data.AnimatorNode, AnimatorNode.KernelPorts.LayerBufferInput);
            set.Connect(data.EntityNode, ComponentNode.Output<AnimatorStateBuffer>(), data.AnimatorNode, AnimatorNode.KernelPorts.StateBufferInput);
            set.Connect(data.AnimatorNode, AnimatorNode.KernelPorts.Output, data.EntityNode, NodeSetAPI.ConnectionType.Feedback);
            data.SpeedGraphValueArray = set.CreateGraphValueArray(data.AnimatorNode, AnimatorNode.KernelPorts.SpeedBufferOutput);

            ref var nodeData = ref setup.ValueRef.Value;
            var layerParamBuffer = pcb.AddBuffer<AnimatorLayerBuffer>(entity);
            layerParamBuffer.Length = nodeData.layerDatas.Length;
            var stateParamBuffer = pcb.AddBuffer<AnimatorStateBuffer>(entity);
            stateParamBuffer.Length = nodeData.totalStateCount;
            for (var i = 0; i < layerParamBuffer.Length; i++)
            {
                layerParamBuffer[i] = new AnimatorLayerBuffer()
                {
                    Weight = i == 0 ? 1 : 0
                };
            }

            for (var i = 0; i < stateParamBuffer.Length; i++)
            {
                stateParamBuffer[i] = new AnimatorStateBuffer()
                {
                    Time = 0,
                    Weight = i == 0 ? 1 : 0,
                    ParamX = 0,
                    ParamY = 0
                };
            }

            ref var rawData = ref setup.ValueRef.Value;
            using (var builder = new BlobBuilder(Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<AnimatorNodeData>();
                root.totalStateCount = rawData.totalStateCount;
                var layerDatas = builder.Allocate(ref root.layerDatas, rawData.layerDatas.Length);
                for (var layerIdx = 0; layerIdx < layerDatas.Length; layerIdx++)
                {
                    ref var layer = ref layerDatas[layerIdx];
                    ref var layerRaw = ref rawData.layerDatas[layerIdx];
                    layer.ChannelWeightTableRef = layerRaw.ResourceId >= 0 ? maskRes[layerRaw.ResourceId].ValueRef : default;
                    var stateDatas = builder.Allocate(ref layer.stateDatas, layerRaw.stateDatas.Length);

                    for (var stateIdx = 0; stateIdx < stateDatas.Length; stateIdx++)
                    {
                        ref var state = ref stateDatas[stateIdx];
                        ref var stateRaw = ref layerRaw.stateDatas[stateIdx];
                        state.Hash = stateRaw.Hash;
                        state.Type = stateRaw.Type;
                        switch (state.Type)
                        {
                            case AnimatorStateType.Clip:
                                state.ResourceRef = clipRes[stateRaw.ResourceId].ValueRef;
                                break;
                            case AnimatorStateType.Blend1D:
                                state.ResourceRef = BlendTreeBuilder.CreateBlendTree1DFromComponents(b1dRes[stateRaw.ResourceId], EntityManager, entity);
                                break;
                            case AnimatorStateType.Blend2D:
                                state.ResourceRef = BlendTreeBuilder.CreateBlendTree2DFromComponents(b2dRes[stateRaw.ResourceId], EntityManager, entity);
                                break;
                        }
                    }
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
            in AnimatorGraphData data)
        {
            var set = graphSystem.Set;
            set.ReleaseGraphValueArray(data.SpeedGraphValueArray);
            set.Destroy(data.AnimatorNode);
            set.Destroy(data.EntityNode);
        }
    }

    [UpdateBefore(typeof(DefaultAnimationSystemGroup))]
    [UpdateBefore(typeof(AnimatorGraphSystem))]
    public class UpdateAnimationTime : SystemBase
    {
        protected IAnimationGraphSystem m_AnimationSystem;

        protected override void OnCreate()
        {
            m_AnimationSystem = World.GetOrCreateSystem<ProcessDefaultAnimationGraph>();
            m_AnimationSystem.AddRef();
        }

        protected override void OnDestroy()
        {
            m_AnimationSystem.RemoveRef();
        }
        protected override void OnUpdate()
        {
            var worldDeltaTime = Time.DeltaTime;
            var resolver = m_AnimationSystem.Set.GetGraphValueResolver(out var dep);
            dep = Entities.ForEach((Entity Entity, ref DynamicBuffer<AnimatorStateBuffer> stateParamBuffer, in AnimatorGraphData animatorNodeData) =>
            {
                var speedArr = resolver.Resolve(animatorNodeData.SpeedGraphValueArray);
                for (var i = 0; i < stateParamBuffer.Length; i++)
                {
                    ref var state = ref stateParamBuffer.ElementAt(i);
                    state.Time += speedArr[i] * worldDeltaTime;
                }
            }).ScheduleParallel(dep);
            m_AnimationSystem.Set.InjectDependencyFromConsumer(dep);
            dep.Complete();
        }
    }

    //public static class AnimationUtil
    //{
    //    public static void SetClip(this ref AnimatorLayerResource layerRes, ref AnimatorStateBuffer stateParamBuffer, StringHash hash)
    //    {
    //        for (var i = 0; i < layerRes.StateCount; i++)
    //        {
    //            var stateResIdx = i + layerRes.StateStartIndex;
    //        }
    //    }
    //}
}
