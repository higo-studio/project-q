using Unity.Entities;
using Unity.Animation;
using Unity.Animation.Hybrid;
using Unity.DataFlowGraph;
using Higo.Animation;

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

    public struct AnimationControllerSystemStateData : ISystemStateComponentData
    {
        public NodeHandle<ConvertDeltaTimeToFloatNode> DeltaTimeNode;
        public NodeHandle<TimeCounterNode> TimeCounterNode;
        public NodeHandle<TimeLoopNode> TimeLoopNode;
        public NodeHandle<FloatRcpNode> FloatRcpNode;
    }
}
