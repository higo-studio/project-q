using Unity.Entities;

namespace Higo.Camera
{
    [GenerateAuthoringComponent]
    public struct VirtualCameraComponent : IComponentData {
        public int Priority;
    }
}