using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class BoidMover : JobComponentSystem
{
    [BurstCompile]
    struct MovementJob : IJobForEach<Translation, Rotation, BoidComponent>
    {
        public float DeltaTime;

        public void Execute(ref Translation translation, ref Rotation rotation, [ReadOnly] ref BoidComponent boidComponent)
        {
            var velocity = boidComponent.Velocity;
            velocity += math.normalizesafe(boidComponent.Velocity) * boidComponent.Acceleration * DeltaTime;
            velocity *= 1.0f - 30.0f * boidComponent.Drag * DeltaTime;

            translation.Value += velocity * DeltaTime;
            rotation.Value = quaternion.LookRotationSafe(translation.Value + velocity, math.up());
        }
    }


    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var movementJob = new MovementJob { DeltaTime = Time.DeltaTime };

        return movementJob.Schedule(this, inputDeps);
    }
}