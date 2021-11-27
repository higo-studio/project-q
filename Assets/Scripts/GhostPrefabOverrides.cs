using Unity.NetCode;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

[GhostComponentVariation(typeof(Translation), "TrsWith000andInterpolate")]
[GhostComponent(PrefabType = GhostPrefabType.All, OwnerPredictedSendType = GhostSendType.All, SendDataForChildEntity = false)]
public struct TrsWith000andInterpolate
{
    [GhostField(Quantization = 1000, Smoothing = SmoothingAction.Interpolate, SubType = 0)] public float3 Value;
}
[GhostComponentVariation(typeof(Rotation), "RotWith000andInterpolate")]
[GhostComponent(PrefabType = GhostPrefabType.All, OwnerPredictedSendType = GhostSendType.All, SendDataForChildEntity = false)]
public struct RotWith000andInterpolate
{
    [GhostField(Quantization = 1000, Smoothing = SmoothingAction.Interpolate, SubType = 0)] public quaternion Value;
}