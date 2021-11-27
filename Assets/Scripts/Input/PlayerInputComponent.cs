using Unity.NetCode;
using Unity.Mathematics;

public struct PlayerInputComponent : ICommandData
{
    public float2 Movement;
    public float2 Looking;
    public bool Jump;
    public bool Shift;

    public uint Tick { get; set; }
}