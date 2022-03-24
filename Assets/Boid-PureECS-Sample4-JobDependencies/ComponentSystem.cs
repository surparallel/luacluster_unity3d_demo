using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using grpania_unity3d_demo;

namespace Boid.PureECS.Sample4
{

public class BoidsSimulationSystem : JobComponentSystem
{
    public struct MoveJob : IJobProcessComponentDataWithEntity<Position, Rotation, Velocity, Origin>
    {
        [ReadOnly] public float dt;
        [ReadOnly] public float minSpeed;
        [ReadOnly] public float maxSpeed;
        [ReadOnly] public long current;
        public void Execute(
        Entity entity,
        int index,
        ref Position pos,
        ref Rotation rot,
        ref Velocity velocity,
        ref Origin origin)
        {
            if(velocity.stop != 0 && current > velocity.stop)
            {
                return;
            }

            if (velocity.begin == 0)
            {
                return;
            }
            
            Vector3 right = new float3(0f, 0f, 1f);
            Quaternion q = new Quaternion(rot.Value.value.x, rot.Value.value.y, rot.Value.value.z, rot.Value.value.w);
            Vector3 length = q * right;
            var speed = velocity.Value;

            velocity.dt = velocity.dt + dt;
            length.Scale(new Vector3(speed * velocity.dt, speed * velocity.dt, speed * velocity.dt));
            float3 fv = length;
            pos.Value = origin.Value + fv;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {

        var move = new MoveJob
        {
            dt = Time.deltaTime,
            minSpeed = Bootstrap.Param.minSpeed,
            maxSpeed = Bootstrap.Param.maxSpeed,
            current = Tool.TimeStamp()
        };

        inputDeps = move.Schedule(this, inputDeps);
        return inputDeps;
    }
}

}