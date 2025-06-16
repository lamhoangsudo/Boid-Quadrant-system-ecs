using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

partial struct BoidSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            EntityCommandBuffer entityCommandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (RefRW<BoidSpawn> boidSpawn in SystemAPI.Query<RefRW<BoidSpawn>>())
            {
                for (int i = 0; i < boidSpawn.ValueRO.boidCount; i++)
                {
                    SpawnBoid(entityCommandBuffer, boidSpawn, false);
                }
            }
        }
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            EntityCommandBuffer entityCommandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (RefRW<BoidSpawn> boidSpawn in SystemAPI.Query<RefRW<BoidSpawn>>())
            {
                for (int i = 0; i < boidSpawn.ValueRO.boidLeaderCount; i++)
                {
                    SpawnBoid(entityCommandBuffer, boidSpawn, true);
                }
            }
        }
    }

    private void SpawnBoid(EntityCommandBuffer entityCommandBuffer, RefRW<BoidSpawn> boidSpawn, bool leader)
    {
        // Generate a random spawn position within the specified radius
        float3 spawnPosition = new();
        Unity.Mathematics.Random random = boidSpawn.ValueRO.random;
        spawnPosition.x = random.NextFloat(-boidSpawn.ValueRO.spawnRadius, boidSpawn.ValueRO.spawnRadius);
        spawnPosition.y = random.NextFloat(-boidSpawn.ValueRO.spawnRadius, boidSpawn.ValueRO.spawnRadius);
        spawnPosition.z = random.NextFloat(-boidSpawn.ValueRO.spawnRadius, boidSpawn.ValueRO.spawnRadius);
        // Generate random parameters for the boid
        float speed = random.NextFloat(5f, 10f); // Random speed between 5 and 10
        float rotationSpeed = random.NextFloat(1f, 5f); // Random rotation speed between 1 and 5
        float orbitDistance = random.NextFloat(2f, 4f); // Random orbit distance between 2 and 4
        float targetAttractionWeight = random.NextFloat(0.5f, 1f); // Random target attraction weight between 0.1 and 1
        float alignmentWeight;
        float cohesionWeight;
        float separationWeight;
        float neighborDistance;
        float separationDistance;
        if (leader)
        {
            alignmentWeight = random.NextFloat(1f, 2f); // Random alignment weight between 1 and 2
            cohesionWeight = random.NextFloat(0.5f, 1.5f); // Random cohesion weight between 0.5 and 1.5
            separationWeight = random.NextFloat(1.5f, 3f); // Random separation weight between 1.5 and 3
            neighborDistance = random.NextFloat(4f, 5f); // Random neighbor distance between 4 and 5
            separationDistance = random.NextFloat(1.5f, 3f); // Random separation distance between 1.5 and 3
        }
        else
        {
            alignmentWeight = random.NextFloat(1.5f, 2f); // Random alignment weight between 1.5 and 2
            cohesionWeight = random.NextFloat(1f, 1.5f); // Random cohesion weight between 1 and 1.5
            separationWeight = random.NextFloat(1f, 1.5f); // Random separation weight between 1 and 1.5
            neighborDistance = random.NextFloat(4f, 5f); // Random neighbor distance between 5 and 10
            separationDistance = random.NextFloat(1f, 1.5f); // Random separation distance between 1.5 and 3
        }

        float changeTargetTimeMax = random.NextFloat(1f, 3f); // Random change target time max between 1 and 3
        float3 targetPosition = new(random.NextFloat(-boidSpawn.ValueRO.spawnRadius, boidSpawn.ValueRO.spawnRadius), random.NextFloat(-boidSpawn.ValueRO.spawnRadius, boidSpawn.ValueRO.spawnRadius), random.NextFloat(-boidSpawn.ValueRO.spawnRadius, boidSpawn.ValueRO.spawnRadius));
        boidSpawn.ValueRW.random = random;
        // Create the boid entity with the specified parameters
        Entity boidEntity;
        if (leader)
        {
            boidEntity = entityCommandBuffer.Instantiate(boidSpawn.ValueRO.boidLeaderEntity);
        }
        else
        {
            boidEntity = entityCommandBuffer.Instantiate(boidSpawn.ValueRO.boidEntity);
        }
        // Set the position and components of the boid entity
        entityCommandBuffer.SetComponent<LocalTransform>(boidEntity, LocalTransform.FromPosition(spawnPosition));
        entityCommandBuffer.SetComponent<Boid>(boidEntity, new Boid
        {
            speed = speed,
            rotationSpeed = rotationSpeed,
            orbitDistance = orbitDistance,
            targetAttractionWeight = targetAttractionWeight,
            alignmentWeight = 2f,
            cohesionWeight = 1.5f,
            separationWeight = separationWeight,
            neighborDistance = neighborDistance,
            separationDistance = separationDistance,
            isBoidLeader = leader,
            targetPosition = targetPosition,
            random = new((uint)boidEntity.Index),
            changeTargetTimeMax = changeTargetTimeMax,
            changeTargetTime = changeTargetTimeMax,
        });
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
