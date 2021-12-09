using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Higo.Camera
{
    [UpdateInGroup(typeof(TransformSystemGroup))]
    [UpdateAfter(typeof(TRSToLocalToParentSystem))]
    [UpdateBefore(typeof(TRSToLocalToWorldSystem))]
    public class CameraSystemGroup : ComponentSystemGroup
    {
    }
}