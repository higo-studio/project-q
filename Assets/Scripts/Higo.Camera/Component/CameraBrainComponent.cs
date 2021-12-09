using Unity.Entities;
namespace Higo.Camera
{
    [GenerateAuthoringComponent]
    public struct CameraBrainComponent : IComponentData
    {
        public Entity CurrentCamera;
    }
}
