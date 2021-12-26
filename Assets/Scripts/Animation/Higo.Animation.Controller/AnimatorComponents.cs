using Unity.Entities;
using Unity.Animation;

namespace Higo.Animation.Controller
{
    public struct ClipResource : IBufferElementData
    {
        public float MotionSpeed;
        public BlobAssetReference<Unity.Animation.Clip> Motion;
    }

    public struct AnimationStateBuffer : IBufferElementData
    {
        
    }

    public struct AnimationLayerBuffer : IBufferElementData
    {
        public Entity Value;
    }
}
