using Unity.Entities;
using Unity.NetCode;

public class NetCodeBootstrap : ClientServerBootstrap
{
    public override bool Initialize(string defaultWorldName)
    {
        // For the sample scenes we use a dynamic assembly list so we can build a server with a subset of the assemblies
        // (only including one of the samples instead of all)
        RpcSystem.DynamicAssemblyList = true;
        TypeManager.Initialize();
        var success = base.Initialize(defaultWorldName);
        RpcSystem.DynamicAssemblyList = false;
        return success;
    }
}
