using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Animation;

namespace Higo.Animation.Controller
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct BlendTree1DRef
    {
        [FieldOffset(0)]
        private long m_BlobAssetRefStorage;
        public ref BlendTree1D Value => ref UnsafeUtility.As<long, BlobAssetReference<BlendTree1D>>(ref m_BlobAssetRefStorage).Value;
        public static implicit operator BlendTree1DRef(BlobAssetReference<BlendTree1D> assetRef)
        {
            BlendTree1DRef ret = default;
            UnsafeUtility.As<long, BlobAssetReference<BlendTree1D>>(ref ret.m_BlobAssetRefStorage) = assetRef;
            return ret;
        }

        public static implicit operator BlobAssetReference<BlendTree1D>(BlendTree1DRef refref)
        {
            return UnsafeUtility.As<long, BlobAssetReference<BlendTree1D>>(ref refref.m_BlobAssetRefStorage);
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct BlendTree2DSimpleDirectionalRef
    {
        [FieldOffset(0)]
        private long m_BlobAssetRefStorage;
        public ref BlendTree2DSimpleDirectional Value => ref UnsafeUtility.As<long, BlobAssetReference<BlendTree2DSimpleDirectional>>(ref m_BlobAssetRefStorage).Value;
        public static implicit operator BlendTree2DSimpleDirectionalRef(BlobAssetReference<BlendTree2DSimpleDirectional> assetRef)
        {
            BlendTree2DSimpleDirectionalRef ret = default;
            UnsafeUtility.As<long, BlobAssetReference<BlendTree2DSimpleDirectional>>(ref ret.m_BlobAssetRefStorage) = assetRef;
            return ret;
        }

        public static implicit operator BlobAssetReference<BlendTree2DSimpleDirectional>(BlendTree2DSimpleDirectionalRef refref)
        {
            return UnsafeUtility.As<long, BlobAssetReference<BlendTree2DSimpleDirectional>>(ref refref.m_BlobAssetRefStorage);
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct ChannelWeightTableRef
    {
        [FieldOffset(0)]
        private long m_BlobAssetRefStorage;
        public ref ChannelWeightTable Value => ref UnsafeUtility.As<long, BlobAssetReference<ChannelWeightTable>>(ref m_BlobAssetRefStorage).Value;
        public static implicit operator ChannelWeightTableRef(BlobAssetReference<ChannelWeightTable> assetRef)
        {
            ChannelWeightTableRef ret = default;
            UnsafeUtility.As<long, BlobAssetReference<ChannelWeightTable>>(ref ret.m_BlobAssetRefStorage) = assetRef;
            return ret;
        }

        public static implicit operator BlobAssetReference<ChannelWeightTable>(ChannelWeightTableRef refref)
        {
            return UnsafeUtility.As<long, BlobAssetReference<ChannelWeightTable>>(ref refref.m_BlobAssetRefStorage);
        }
    }

    public struct AnimatorNodeLayerData
    {
        public int StateCount;
        public int StateStartIndex;
        public int ChannelWeightTableCount;
        public ChannelWeightTableRef ChannelWeightTableRef;

        public static implicit operator AnimatorNodeLayerData(AnimationLayerResource src) => new AnimatorNodeLayerData()
        {
            StateCount = src.StateCount,
            StateStartIndex = src.StateStartIndex,
            ChannelWeightTableCount = src.ChannelWeightTableCount,
            ChannelWeightTableRef = src.ChannelWeightTableRef
        };
    }

    [BurstCompatible]
    public struct AnimatorNodeData
    {
        public BlobArray<BlendTree1DRef> blendTree1Ds;
        public BlobArray<BlendTree2DSimpleDirectionalRef> blendTree2DSDs;
        public BlobArray<Motion> motions;
        public BlobArray<AnimatorNodeLayerData> layerDatas;
        public BlobArray<AnimationStateResource> stateDatas;
    }
}
