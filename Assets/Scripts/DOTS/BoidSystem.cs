using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class BoidSystem : JobComponentSystem
{
    public const float MATCH_VELOCITY_RATE = 1.0f;
    public const float AVOIDANCE_RANGE = 2.0f;
    public const float AVOIDANCE_RATE = 5.0f;
    public const float COHERENCE_RATE = 2.0f;
    public const float VIEW_RANGE = 3.0f;

    private EntityQuery _boidsGroup;

    [BurstCompile]
    struct SimulationJob : IJobForEachWithEntity<BoidComponent, Translation>
    {
        [ReadOnly] public ArchetypeChunkComponentType<Translation> TranslationType;

        [ReadOnly][DeallocateOnJobCompletion] public NativeArray<ArchetypeChunk> Chunks;

        public float DeltaTime;

        public float3 WorldSize;

        public void Execute(Entity entity, int index, ref BoidComponent boid, [ReadOnly] ref Translation translation)
        {
            for (var i = 0; i < Chunks.Length; i++)
            {
                var positions = Chunks[i].GetNativeArray(this.TranslationType);

                var dt = DeltaTime;
                AvoidInsideBoundsOfCube(ref boid, translation, WorldSize, VIEW_RANGE, dt);
                // TODO: Implement Teams
                //MatchVelocity(ref boid, index, positions, MATCH_VELOCITY_RATE, dt);
                UpdateCoherence(ref boid, translation, index, positions, COHERENCE_RATE, dt);
                AvoidOthers(ref boid, translation, index, AVOIDANCE_RANGE, positions, AVOIDANCE_RATE, dt);
            }
        }

        public void UpdateCoherence(
            ref BoidComponent boid,
            Translation boidPosition,
            int boidIndex,
            NativeArray<Translation> allBoids,
            float coherenceRate,
            float dt)
        {
            if (allBoids.Length < 2)
                return;

            float3 center = float3.zero;
            for (var i = 0; i < allBoids.Length; i++)
            {
                if (i == boidIndex)
                    continue;

                center = allBoids[i].Value;
                break;
            }

            for (var i = 0; i < allBoids.Length; i++)
            {
                var delta = boidPosition.Value - allBoids[i].Value;
                if (delta.Equals(float3.zero))
                    continue;

                center += allBoids[i].Value;
            }
            center *= 1.0f / allBoids.Length - 1;
            boid.Velocity += (center - boidPosition.Value) * coherenceRate * dt;

        }

        public void AvoidOthers(
            ref BoidComponent boid,
            Translation boidPosition,
            int boidIndex,
            float minDist,
            NativeArray<Translation> allBoids,
            float avoidanceRate,
            float dt)
        {
            if (allBoids.Length < 2)
                return;

            var myPosition = boidPosition.Value;
            var minDistSqr = minDist * minDist;
            float3 step = Vector3.zero;
            for (var i = 0; i < allBoids.Length; i++)
            {
                if (i == boidIndex)
                    continue;

                var delta = myPosition - allBoids[i].Value;
                var deltaSqr = math.sqrt(math.length(delta));
                if (deltaSqr > 0 && deltaSqr < minDistSqr)
                {
                    step += delta / Mathf.Sqrt(deltaSqr);
                }
            }
            boid.Velocity += step * avoidanceRate * dt;
        }

        public void MatchVelocity(
            ref BoidComponent boid,
            int boidIndex,
            NativeArray<Translation> allBoids,
            float matchRate,
            float dt)
        {
            if (allBoids.Length < 2)
                return;

            float3 velocity = Vector3.zero;
            for (var i = 0; i < allBoids.Length; i++)
            {
                if (i == boidIndex)
                    continue;

                velocity += allBoids[i].Value;
            }
            velocity /= (allBoids.Length - 1);
            boid.Velocity += (velocity - boid.Velocity) * matchRate * dt;

        }

        public void AvoidInsideBoundsOfCube(
            ref BoidComponent boid,
            Translation boidPosition,
            float3 halfCubeSize,
            float avoidRange,
            float dt)
        {
            boid.Velocity -= new float3(
                Mathf.Max(Mathf.Abs(boidPosition.Value.x) - halfCubeSize.x + avoidRange, 0) * Mathf.Sign(boidPosition.Value.x) * 5f * dt,
                Mathf.Max(Mathf.Abs(boidPosition.Value.y) - halfCubeSize.y + avoidRange, 0) * Mathf.Sign(boidPosition.Value.y) * 5f * dt,
                Mathf.Max(Mathf.Abs(boidPosition.Value.z) - halfCubeSize.z + avoidRange, 0) * Mathf.Sign(boidPosition.Value.z) * 5f * dt);
        }
    }

    protected override void OnCreate()
    {
        var boidQuery = new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(BoidComponent), typeof(Translation) },
        };
        _boidsGroup = GetEntityQuery(boidQuery);
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var translationType = GetArchetypeChunkComponentType<Translation>(true);
        var chunks = _boidsGroup.CreateArchetypeChunkArray(Allocator.TempJob);
        var worldSize =
            Mathf.CeilToInt(
                Mathf.Pow(Spawner.NumberOfBoids, 1f / 3) *
                Spawner.BOID_DENSITY /
                Spawner.ROUND_WORLD_SIZE_TO_MULTIPLES_OF) *
            Spawner.ROUND_WORLD_SIZE_TO_MULTIPLES_OF;

        var simulationJob = new SimulationJob
        {
            Chunks = chunks,
            TranslationType = translationType,
            DeltaTime = Time.DeltaTime,
            WorldSize = new float3(worldSize, worldSize, worldSize)
        };

        return simulationJob.Schedule(this, inputDeps);
    }


}