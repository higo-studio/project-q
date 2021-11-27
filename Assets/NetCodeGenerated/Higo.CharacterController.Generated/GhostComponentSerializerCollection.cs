//THIS FILE IS AUTOGENERATED BY GHOSTCOMPILER. DON'T MODIFY OR ALTER.
using Unity.Entities;
using Unity.NetCode;
using Higo.CharacterController.Generated;

namespace Higo.CharacterController.Generated
{
    [UpdateInGroup(typeof(ClientAndServerInitializationSystemGroup))]
    public class GhostComponentSerializerRegistrationSystem : SystemBase
    {
        protected override void OnCreate()
        {
            var ghostCollectionSystem = World.GetOrCreateSystem<GhostCollectionSystem>();
            ghostCollectionSystem.AddSerializer(CharacterControllerComponentDataGhostComponentSerializer.State);
            ghostCollectionSystem.AddSerializer(CharacterControllerInternalDataGhostComponentSerializer.State);
            ghostCollectionSystem.AddSerializer(CharacterControllerTransformGhostComponentSerializer.State);
        }

        protected override void OnUpdate()
        {
            var parentGroup = World.GetExistingSystem<InitializationSystemGroup>();
            if (parentGroup != null)
            {
                parentGroup.RemoveSystemFromUpdateList(this);
            }
        }
    }
}