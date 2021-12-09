using Unity.Entities;

namespace Higo.Camera
{
    public struct VirtualCameraComponent : IComponentData
    {
        public int Priority;
        public Entity Follow;
        public Entity LookAt;
    }

    public struct CameraActiveRequest : IComponentData { }

    public enum CameraActiveState : byte
    {
        Waiting,
        StandBy,
        Running,

    }
    public struct CameraActiveComponent : IComponentData
    {
        public CameraActiveState Value;
    }
}