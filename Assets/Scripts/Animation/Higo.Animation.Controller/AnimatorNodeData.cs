using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Animation;
using System;

namespace Higo.Animation.Controller
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct AnimatorUntypeedRef
    {
        [FieldOffset(0)]
        private long m_BlobAssetRefStorage;
        public ref T Value<T>() where T : struct => ref UnsafeUtility.As<long, BlobAssetReference<T>>(ref m_BlobAssetRefStorage).Value;
        public static implicit operator AnimatorUntypeedRef(BlobAssetReference<BlendTree1D> assetRef)
        {
            AnimatorUntypeedRef ret = default;
            UnsafeUtility.As<long, BlobAssetReference<BlendTree1D>>(ref ret.m_BlobAssetRefStorage) = assetRef;
            return ret;
        }

        public static implicit operator BlobAssetReference<BlendTree1D>(AnimatorUntypeedRef refref)
        {
            return UnsafeUtility.As<long, BlobAssetReference<BlendTree1D>>(ref refref.m_BlobAssetRefStorage);
        }

        public static implicit operator AnimatorUntypeedRef(BlobAssetReference<BlendTree2DSimpleDirectional> assetRef)
        {
            AnimatorUntypeedRef ret = default;
            UnsafeUtility.As<long, BlobAssetReference<BlendTree2DSimpleDirectional>>(ref ret.m_BlobAssetRefStorage) = assetRef;
            return ret;
        }

        public static implicit operator BlobAssetReference<BlendTree2DSimpleDirectional>(AnimatorUntypeedRef refref)
        {
            return UnsafeUtility.As<long, BlobAssetReference<BlendTree2DSimpleDirectional>>(ref refref.m_BlobAssetRefStorage);
        }

        public static implicit operator AnimatorUntypeedRef(BlobAssetReference<ChannelWeightTable> assetRef)
        {
            AnimatorUntypeedRef ret = default;
            UnsafeUtility.As<long, BlobAssetReference<ChannelWeightTable>>(ref ret.m_BlobAssetRefStorage) = assetRef;
            return ret;
        }

        public static implicit operator BlobAssetReference<ChannelWeightTable>(AnimatorUntypeedRef refref)
        {
            return UnsafeUtility.As<long, BlobAssetReference<ChannelWeightTable>>(ref refref.m_BlobAssetRefStorage);
        }

        public static implicit operator AnimatorUntypeedRef(BlobAssetReference<Clip> assetRef)
        {
            AnimatorUntypeedRef ret = default;
            UnsafeUtility.As<long, BlobAssetReference<Clip>>(ref ret.m_BlobAssetRefStorage) = assetRef;
            return ret;
        }

        public static implicit operator BlobAssetReference<Clip>(AnimatorUntypeedRef clip)
        {
            return UnsafeUtility.As<long, BlobAssetReference<Clip>>(ref clip.m_BlobAssetRefStorage);
        }
    }

    public struct AnimatorLayerData
    {
        public int ResourceId;
        public AnimatorUntypeedRef ChannelWeightTableRef;
        public BlobArray<AnimatorStateData> stateDatas;
    }

    public struct AnimatorStateData
    {
        public StringHash Hash;
        public AnimatorStateType Type;
        public AnimatorUntypeedRef ResourceRef;
        public int ResourceId;
        public int IdInBuffer;
        public float Duration;
        public BlobArray<AnimatorTransitionInfo> transitionInfos;
    }

    [Serializable]
    public struct AnimatorTransitionInfo
    {
        public float NextStateIndex;
        public float ExitTime;
        public float TransitionTime;
        public float EnterTime;
    }

    [BurstCompatible]
    public struct AnimatorNodeData
    {
        public int totalStateCount;
        public BlobArray<AnimatorLayerData> layerDatas;
    }
}
