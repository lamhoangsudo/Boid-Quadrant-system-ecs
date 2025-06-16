using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class BoidAuthoring : MonoBehaviour
{
    [SerializeField] private float neighborDistance;
    [SerializeField] private float separationDistance;
    [SerializeField] private float alignmentWeight;
    [SerializeField] private float cohesionWeight;
    [SerializeField] private float separationWeight;
    [SerializeField] private float orbitDistance;
    [SerializeField] private float speed;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float targetAttractionWeight;
    [SerializeField] private float changeTargetTimeMax;
    [SerializeField] private bool isBoidLeader = false;
    public class BoidAuthoringBaker : Baker<BoidAuthoring>
    {
        public override void Bake(BoidAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Boid
            {
                alignmentWeight = authoring.alignmentWeight,
                cohesionWeight = authoring.cohesionWeight,
                neighborDistance = authoring.neighborDistance,
                separationWeight = authoring.separationWeight,
                orbitDistance = authoring.orbitDistance,
                speed = authoring.speed,
                rotationSpeed = authoring.rotationSpeed,
                targetAttractionWeight = authoring.targetAttractionWeight,
                separationDistance = authoring.separationDistance,
                changeTargetTimeMax = authoring.changeTargetTimeMax,
                changeTargetTime = authoring.changeTargetTimeMax,
                random = new((uint)entity.Index),
                isBoidLeader = authoring.isBoidLeader,
            });
        }
    }
}
public struct Boid : IComponentData
{
    public float3 direction;
    public float3 targetPosition;
    public float neighborDistance;
    public float alignmentWeight;
    public float cohesionWeight;
    public float separationWeight;
    public float orbitDistance;
    public float speed;
    public float targetAttractionWeight;
    public float rotationSpeed;
    public float separationDistance;
    public float changeTargetTime;
    public float changeTargetTimeMax;
    public Unity.Mathematics.Random random;
    public bool isBoidLeader;
}


