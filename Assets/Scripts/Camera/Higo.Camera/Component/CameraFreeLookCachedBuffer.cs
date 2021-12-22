using Unity.Entities;
using Unity.Mathematics;
namespace Higo.Camera
{
    public struct CameraFreeLookCachedBuffer : IBufferElementData
    {
        public float4 knot;
        public float4 ctrl1;
        public float4 ctrl2;
    }
}
