using Unity.Entities;
using Unity.Animation;
using Unity.NetCode;

namespace Higo.Animation
{
    [UpdateAfter(typeof(LateAnimationSystemGroup))]
    [UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
    public class BoneRendererSystemGroup : ComponentSystemGroup
    {
    }

    [UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
    [UpdateInGroup(typeof(BoneRendererSystemGroup))]

    public class BoneRendererMatrixSystem : ComputeBoneRenderingMatricesBase
    {
    }

    [UpdateInGroup(typeof(BoneRendererSystemGroup))]
    [UpdateAfter(typeof(BoneRendererMatrixSystem))]
    [UpdateInWorld(UpdateInWorld.TargetWorld.Client)]

    public class BoneRendererRenderingSystem : RenderBonesBase
    {
    }
}
