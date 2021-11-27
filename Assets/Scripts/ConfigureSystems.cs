using Unity.Entities;
using Unity.NetCode;
using Unity.Scenes;
using Hash128 = Unity.Entities.Hash128;

#if UNITY_CLIENT || UNITY_EDITOR
[UpdateInGroup(typeof(ClientInitializationSystemGroup))]
public class ConfigureClientSystems : SystemBase
{
    public static Hash128 ClientBuildSettingsGUID => new Hash128("e41813f9f3a00b441bc313389dd82d48");

    protected override void OnCreate()
    {
        World.GetOrCreateSystem<SceneSystem>().BuildConfigurationGUID = ClientBuildSettingsGUID;
    }

    protected override void OnUpdate()
    {
    }
}
#endif

#if UNITY_SERVER || UNITY_EDITOR
[UpdateInGroup(typeof(ServerInitializationSystemGroup))]
public class ConfigureServerSystems : SystemBase
{
    public static Hash128 ServerBuildSettingsGUID => new Hash128("19e59ee2b4a64aa4eb73a91682c0388b");

    protected override void OnCreate()
    {
        World.GetOrCreateSystem<SceneSystem>().BuildConfigurationGUID = ServerBuildSettingsGUID;
    }

    protected override void OnUpdate()
    {
    }
}
#endif
