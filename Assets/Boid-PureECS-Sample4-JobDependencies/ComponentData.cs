using Unity.Entities;
using Unity.Mathematics;

namespace Boid.PureECS.Sample4
{

public struct Velocity : IComponentData
{
    public float Value;
    public uint begin;
    public uint stop;
    public float dt;
    }

public struct Origin : IComponentData
{
    public float3 Value;
}

}