using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Higo.Camera
{
    [UpdateInGroup(typeof(TransformSystemGroup))]
    [UpdateAfter(typeof(EndFrameWorldToLocalSystem))]
    public class CameraLateUpdateGroup : ComponentSystemGroup
    {
    }
}