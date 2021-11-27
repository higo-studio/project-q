using Unity.Entities;
using Unity.Collections;

namespace Unity.NetCode
{
    [UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
    public class NetworkStreamCloseSystem : SystemBase
    {
        private BeginSimulationEntityCommandBufferSystem m_Barrier;

        protected override void OnCreate()
        {
            m_Barrier = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var commandBuffer = m_Barrier.CreateCommandBuffer();
            FixedString64 worldName = World.Name;

            FixedString4096 s = new FixedString4096();
            Entities/* .WithAll<NetworkStreamDisconnected>() */.ForEach((Entity entity, in NetworkStreamConnection con, in NetworkStreamDisconnected disconnected) =>
            {
                UnityEngine.Debug.LogError(
                FixedString.Format("asd {1} Network Stream Disconnect, reason: {0}", (int)disconnected.Reason, worldName));
                commandBuffer.DestroyEntity(entity);
            }).Schedule();
            m_Barrier.AddJobHandleForProducer(Dependency);
        }
    }
}
