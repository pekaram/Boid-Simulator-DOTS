using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

public class Spawner : MonoBehaviour
{
    public const int NumberOfBoids = 10;
    public const float BOID_DENSITY = 4f;
    public const int ROUND_WORLD_SIZE_TO_MULTIPLES_OF = 5;

    [SerializeField]
    private Material _material;

    [SerializeField]
    private Mesh _mesh;

    private EntityManager _entityManager;

    private EntityArchetype _boidEntityArchetype;

    private void Awake()
    {
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _boidEntityArchetype = _entityManager.CreateArchetype(
            typeof(Translation),
            typeof(Rotation),
            typeof(LocalToWorld),
            typeof(RenderMesh),
            typeof(RenderBounds),
            typeof(BoidComponent)
        );
    }

    private void Start()
    {
        for (var i = 0; i < NumberOfBoids; i++)
        {
            SpawnRandomBoid();
        }
    }

    private void SpawnRandomBoid()
    {
        var entity = _entityManager.CreateEntity(_boidEntityArchetype);

        _entityManager.AddComponentData(entity, new Translation
        {
            Value = new float3(Random.Range(1, 11), Random.Range(1, 11), Random.Range(1, 11))
        });

        _entityManager.AddComponentData(entity, new Rotation());

        _entityManager.AddComponentData(entity, new BoidComponent()
        {
            Velocity = new float3(Random.Range(1, 11), Random.Range(1, 11), Random.Range(1, 11)),
            Acceleration = Random.Range(1, 11),
            Drag = Random.Range(0.01f, 0.04f),
        });

        _entityManager.AddSharedComponentData(entity, new RenderMesh
        {
            mesh = _mesh,
            material = _material,
        });
    }
}