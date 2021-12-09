using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Higo.Camera
{
    [UpdateInGroup(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(CopyTransformToGameObjectSystem))]
    [UpdateAfter(typeof(EndFrameWorldToLocalSystem))]
    public class CameraSystemGroup : ComponentSystemGroup
    {
    }
}