using Unity.Entities;
using Unity.Mathematics;

public struct BoidComponent : IComponentData
{
    public float3 Velocity { get; set; }

    public float Acceleration { get; set; }

    public float Drag { get; set; }
}