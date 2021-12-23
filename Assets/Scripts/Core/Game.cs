using System;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Burst;
using PlayType = Unity.NetCode.ClientServerBootstrap.PlayType;


[UpdateInWorld(UpdateInWorld.TargetWorld.Default)]
public class Game : SystemBase
{
    // Singleton component to trigger connections once from a control system
    struct InitGameComponent : IComponentData
    {
    }
    protected override void OnCreate()
    {
        RequireSingletonForUpdate<InitGameComponent>();
        // Create singleton, require singleton for update so system runs once
        EntityManager.CreateEntity(typeof(InitGameComponent));
    }

    void SetupTickRate(World world)
    {
        var tickrateEnt = world.EntityManager.CreateEntity(typeof(ClientServerTickRate));
        var tick = default(ClientServerTickRate);
        tick.SimulationTickRate = 30;
        tick.ResolveDefaults();
        world.EntityManager.SetComponentData(tickrateEnt, tick);
    }

    protected override void OnUpdate()
    {
        // Destroy singleton to prevent system from running again
        EntityManager.DestroyEntity(GetSingletonEntity<InitGameComponent>());
        var playType = ClientServerBootstrap.RequestedPlayType;
        foreach (var world in World.All)
        {
            var network = world.GetExistingSystem<NetworkStreamReceiveSystem>();
            if (world.GetExistingSystem<ClientSimulationSystemGroup>() != null)
            {
                SetupTickRate(world);
#if UNITY_EDITOR
                if (playType == PlayType.ClientAndServer
                    || (playType == PlayType.Client && !string.IsNullOrWhiteSpace(ClientServerBootstrap.RequestedAutoConnect)))
                {
                    var ep = NetworkEndPoint.Parse(ClientServerBootstrap.RequestedAutoConnect, 7979);
                    network.Connect(ep);
                }
#endif
            }
#if UNITY_EDITOR || UNITY_SERVER
            else if (world.GetExistingSystem<ServerSimulationSystemGroup>() != null)
            {
                SetupTickRate(world);
                // Server world automatically listen for connections from any host
                NetworkEndPoint ep = NetworkEndPoint.AnyIpv4;
                ep.Port = 7979;
                network.Listen(ep);
            }
#endif
        }
    }

}
