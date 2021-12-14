using Unity.Entities;
using Unity.Mathematics;

namespace Higo.Camera
{
    public struct CameraBrainComponent : IComponentData
    {
        public Entity CurrentCamera;
    }

    public struct CameraControllerComponent : IComponentData
    {
        public float2 Axis;
    }
}
