using Unity.Entities;
using Unity.Animation;
using Unity.Animation.Hybrid;
using Unity.DataFlowGraph;
using Higo.Animation;
using Unity.Collections.LowLevel;
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

    public struct AnimationStateResource : IBufferElementData
    {
        public StringHash Hash;
        public int ResourceId;
        public AnimationStateType Type;
    }

    public struct AnimationLayerResource : IBufferElementData
    {
        public int StateCount;
        public int StateStartIndex;
        public int ChannelWeightTableCount;
        public BlobAssetReference<ChannelWeightTable> ChannelWeightTableRef;
    }

    public struct AnimationControllerSystemStateGraphData : ISystemStateComponentData
    {
        public NodeHandle<ComponentNode> EntityNode;
        public NodeHandle<ConvertDeltaTimeToFloatNode> DeltaTimeNode;
        public NodeHandle<TimeCounterNode> TimeCounterNode;
        public NodeHandle<TimeLoopNode> TimeLoopNode;
        public NodeHandle<FloatRcpNode> FloatRcpNode;
        public NodeHandle<AnimatorNode> AnimatorNode;
    }

    public struct AnimationControllerLayerParamBuffer : IBufferElementData
    {
        public float Weight;
    }

    public struct AnimationControllerStateParamBuffer : IBufferElementData
    {
        public float Weight;
        public float ParamX;
        public float ParamY;
    }

    // public struct AnimatorBlob
    // {
    //     public BlobArray<BlendTree1D> blend1Ds;
    //     public BlobArray<BlendTree2DSimpleDirectional> blend2Ds;
    //     public BlobArray<Clip> clips;
    // }
}
