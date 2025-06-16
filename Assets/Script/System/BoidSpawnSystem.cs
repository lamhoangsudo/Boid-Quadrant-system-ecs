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
        state.RequireForUpdate<BoidSpawn>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            EntityCommandBuffer entityCommandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (RefRW<BoidSpawn> boidSpawn in SystemAPI.Query<RefRW<BoidSpawn>>())
            {
                Boid boid = SystemAPI.GetComponent<Boid>(boidSpawn.ValueRO.boidEntity);
                for (int i = 0; i < boidSpawn.ValueRO.boidCount; i++)
                {
                    SpawnBoid(entityCommandBuffer, boidSpawn, false, boid);
                }
            }
        }
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            EntityCommandBuffer entityCommandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (RefRW<BoidSpawn> boidSpawn in SystemAPI.Query<RefRW<BoidSpawn>>())
            {
                Boid boid = SystemAPI.GetComponent<Boid>(boidSpawn.ValueRO.boidEntity);
                for (int i = 0; i < boidSpawn.ValueRO.boidLeaderCount; i++)
                {
                    SpawnBoid(entityCommandBuffer, boidSpawn, true, boid);
                }
            }
        }
    }

    private void SpawnBoid(EntityCommandBuffer entityCommandBuffer, RefRW<BoidSpawn> boidSpawn, bool leader, Boid boidComponent)
    {
        // Generate a random spawn position within the specified radius
        
        Unity.Mathematics.Random random = boidSpawn.ValueRO.random;
        float3 spawnPosition = new(
            random.NextFloat(-boidSpawn.ValueRO.spawnRadius, boidSpawn.ValueRO.spawnRadius), 
            random.NextFloat(-boidSpawn.ValueRO.spawnRadius, boidSpawn.ValueRO.spawnRadius), 
            random.NextFloat(-boidSpawn.ValueRO.spawnRadius, boidSpawn.ValueRO.spawnRadius)
            );
        float3 targetPosition = new(
            random.NextFloat(-boidSpawn.ValueRO.spawnRadius, boidSpawn.ValueRO.spawnRadius), 
            random.NextFloat(-boidSpawn.ValueRO.spawnRadius, boidSpawn.ValueRO.spawnRadius), 
            random.NextFloat(-boidSpawn.ValueRO.spawnRadius, boidSpawn.ValueRO.spawnRadius)
            );
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
            speed = boidComponent.speed,
            rotationSpeed = boidComponent.rotationSpeed,
            orbitDistance = boidComponent.orbitDistance,
            targetAttractionWeight = boidComponent.targetAttractionWeight,
            alignmentWeight = boidComponent.alignmentWeight,
            cohesionWeight = boidComponent.cohesionWeight,
            separationWeight = boidComponent.separationWeight,
            neighborDistance = boidComponent.neighborDistance,
            separationDistance = boidComponent.separationDistance,
            isBoidLeader = leader,
            targetPosition = targetPosition,
            random = new((uint)boidEntity.Index),
            changeTargetTimeMax = boidComponent.changeTargetTimeMax,
            changeTargetTime = boidComponent.changeTargetTimeMax,
        });
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
