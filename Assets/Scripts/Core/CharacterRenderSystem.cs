using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
public class CharacterRenderInitSys : SystemBase
{
    BeginSimulationEntityCommandBufferSystem m_BeginSimulationEcbSystem;
    protected override void OnCreate()
    {
        m_BeginSimulationEcbSystem = World
            .GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var pcb = m_BeginSimulationEcbSystem.CreateCommandBuffer().AsParallelWriter();
        Entities.WithNone<CharacterRenderStateComponent>().ForEach((Entity entity, int entityInQueryIndex, in CharacterComponent c) =>
        {
            var renderEnt = pcb.Instantiate(entityInQueryIndex, c.RenderPrefab);
            pcb.AddComponent(entityInQueryIndex, entity, new CharacterRenderStateComponent()
            {
                Value = renderEnt
            });
        }).Schedule();
        m_BeginSimulationEcbSystem.AddJobHandleForProducer(Dependency);
    }
}

[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
[UpdateInGroup(typeof(TransformSystemGroup))]
[UpdateAfter(typeof(EndFrameTRSToLocalToWorldSystem))]
[UpdateBefore(typeof(EndFrameWorldToLocalSystem))]
public class CharacterRenderUpdateSys : SystemBase
{
    EndSimulationEntityCommandBufferSystem m_EndSimulationEcbSystem;
    protected override void OnUpdate()
    {
        var pcb = new EntityCommandBuffer(Unity.Collections.Allocator.TempJob);
        var pwriter = pcb.AsParallelWriter();
        Entities.ForEach((int entityInQueryIndex, in LocalToWorld l2w, in CharacterRenderStateComponent crs) =>
        {
            pwriter.SetComponent(entityInQueryIndex, crs.Value, new LocalToWorld()
            {
                Value = l2w.Value
            });
        }).ScheduleParallel();
        CompleteDependency();
        pcb.Playback(EntityManager);
        pcb.Dispose();
    }
}


[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
public class CharacterRenderDestroySys : SystemBase
{
    EndSimulationEntityCommandBufferSystem m_EndSimulationEcbSystem;
    protected override void OnCreate()
    {
        m_EndSimulationEcbSystem = World
            .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        var pcb = m_EndSimulationEcbSystem.CreateCommandBuffer().AsParallelWriter();
        Entities.WithNone<CharacterComponent>().ForEach((Entity entity, int entityInQueryIndex, in CharacterRenderStateComponent crs) =>
        {
            pcb.DestroyEntity(entityInQueryIndex, crs.Value);
            pcb.RemoveComponent<CharacterRenderStateComponent>(entityInQueryIndex, entity);
        }).Schedule();
        m_EndSimulationEcbSystem.AddJobHandleForProducer(Dependency);
    }
}