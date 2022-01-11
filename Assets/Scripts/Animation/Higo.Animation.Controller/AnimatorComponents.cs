using Unity.Entities;
using Unity.Animation;
using Unity.Animation.Hybrid;
using Unity.DataFlowGraph;
using Higo.Animation;
using Unity.Collections.LowLevel;
namespace Higo.Animation.Controller
{
    public enum AnimatorStateType
    {
        Clip, Blend1D, Blend2D
    }
    public struct AnimatorClipResource : IBufferElementData
    {
        public float MotionSpeed;
        public BlobAssetReference<Unity.Animation.Clip> ValueRef;
    }

    public struct AnimatorRigMaskResource : IBufferElementData
    {
        public BlobAssetReference<ChannelWeightTable> ValueRef;
        public int EntryCount;
    }

    public struct AnimatorSetup : IComponentData
    {
        public BlobAssetReference<AnimatorNodeDataRaw> ValueRef;
    }

    public struct AnimatorGraphData : ISystemStateComponentData
    {
        public NodeHandle<ComponentNode> EntityNode;
        public NodeHandle<AnimatorNode> AnimatorNode;
        public GraphValueArray<float> SpeedGraphValueArray;
    }

    public struct AnimatorLayerBuffer : IBufferElementData
    {
        public float Weight;
    }

    public struct AnimatorStateBuffer : IBufferElementData
    {
        public float Time;
        public float Weight;
        public float ParamX;
        public float ParamY;
    }
}
