using Unity.Entities;
using Unity.Animation;

namespace Higo.Animation.Controller
{
    public enum AnimationStateType
    {
        Clip, Blend1D, Blend2D
    }
    public struct ClipResource : IBufferElementData
    {
        public float MotionSpeed;
        public BlobAssetReference<Unity.Animation.Clip> Motion;
    }

    public struct AnimationStateBuffer : IBufferElementData
    {
        public int ResourceId;
        public AnimationStateType Type;
    }

    public struct AnimationLayerBuffer : IBufferElementData
    {
        public int StateCount;
    }

    public struct AnimationControllerData : ISystemStateComponentData
    {

    }
}
