using Unity.Entities;
using UnityEngine;

public class BoidSpawnAuthoring : MonoBehaviour
{
    [SerializeField] private GameObject boidPrefab;
    [SerializeField] private GameObject boidLeaderPrefab;
    [SerializeField] private int boidCount;
    [SerializeField] private int boidLeader;
    [SerializeField] private float spawnRadius;
    public class NewBakerScriptBaker : Baker<BoidSpawnAuthoring>
    {
        public override void Bake(BoidSpawnAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new BoidSpawn
            {
                boidEntity = GetEntity(authoring.boidPrefab, TransformUsageFlags.Dynamic),
                boidLeaderEntity = GetEntity(authoring.boidLeaderPrefab, TransformUsageFlags.Dynamic),
                boidCount = authoring.boidCount,
                boidLeaderCount = authoring.boidLeader,
                spawnRadius = authoring.spawnRadius,
                random = new((uint)entity.Index),
            });
        }
    }
}
public struct BoidSpawn : IComponentData
{
    public Entity boidEntity;
    public Entity boidLeaderEntity;
    public int boidCount;
    public int boidLeaderCount;
    public float spawnRadius;
    public Unity.Mathematics.Random random;
}


