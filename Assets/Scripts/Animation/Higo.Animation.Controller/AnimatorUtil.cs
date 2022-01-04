using Unity.Entities;
using Unity.Animation;

namespace Higo.Animation.Controller
{
    public static class AnimatorUtil
    {

        public static void SetClip(int layer, StringHash hash,
            in DynamicBuffer<AnimationLayerResource> layerRess,
            in DynamicBuffer<AnimationStateResource> stateRess,
            ref DynamicBuffer<AnimationControllerStateParamBuffer> stateParams
        )
        {
            var layerRes = layerRess[layer];
            for (var stateId = layerRes.StateStartIndex; stateId < layerRes.StateCount + layerRes.StateStartIndex; stateId++)
            {
                
            }
        }
    }
}