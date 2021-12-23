using Unity.NetCode;
using Unity.Mathematics;

public struct PlayerInputComponent : ICommandData
{
    public float2 MovementRaw;
    public float2 Movement;
    public float3 CameraForward;
    public bool Jump;
    public bool Shift;

    public uint Tick { get; set; }
}