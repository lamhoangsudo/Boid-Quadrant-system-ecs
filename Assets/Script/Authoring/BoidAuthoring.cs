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

    
    [SerializeField] private float speed;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float orbitDistance;
    [SerializeField] private float targetAttractionWeight;
    [SerializeField] private float smoothFactor;

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
                smoothFactor = authoring.smoothFactor,
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
    public float3 alignment;
    public float3 cohesion;
    public float3 separation;
    
    public float neighborDistance;
    public float separationDistance;

    public float alignmentWeight;
    public float cohesionWeight;
    public float separationWeight;
    
    public float speed;
    public float rotationSpeed;
    public float orbitDistance;
    public float targetAttractionWeight;
    public float smoothFactor;

    public float changeTargetTime;
    public float changeTargetTimeMax;
    public Unity.Mathematics.Random random;
    public bool isBoidLeader;
}


