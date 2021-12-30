using Unity.Collections;
using Unity.Animation;
using Unity.Entities;
namespace Higo.Animation.Controller
{
    public static class AnimatorConversion
    {
        // public static BlobAssetReference<AnimatorBlob> Convert(BlendTree1DResource[] blend1dResources, BlendTree2DResource[] blend2dResources, ClipResource[] clipResources, EntityManager entityManager, Entity entity)
        // {
        //     using (var builder = new BlobBuilder(Allocator.Temp))
        //     {
        //         ref var animatorBlob = ref builder.ConstructRoot<AnimatorBlob>();
        //         builder.Allocate(ref animatorBlob.blend1Ds, blend1dResources.Length);
        //         builder.Allocate(ref animatorBlob.blend2Ds, blend2dResources.Length);
        //         builder.Allocate(ref animatorBlob.clips, clipResources.Length);

        //         for (var i = 0; i < blend1dResources.Length; i++)
        //         {
        //             var res = blend1dResources[i];
        //             animatorBlob.blend1Ds[i] = BlendTreeBuilder.CreateBlendTree1DFromComponents(res, entityManager, entity).Value;
        //         }
        //         return builder.CreateBlobAssetReference<AnimatorBlob>(Allocator.Persistent);
        //     }
        // }
    }

}