using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static UnityEditor.Searcher.SearcherWindow;
using UnityEngine.UIElements;
using Unity.VisualScripting;
[UpdateBefore(typeof(BoidHashing))]
partial struct BoidCaculaterDirectionSystem : ISystem
{
    private NativeArray<int3> neighborOffsets;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        GetNeighborOffSet();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        BoidHashing boidHashing = SystemAPI.GetSingleton<BoidHashing>();
        foreach ((RefRO<LocalTransform> localTransform, RefRW<Boid> boid, Entity currentBoidEntity) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<Boid>>().WithEntityAccess())
        {
            int neighborCount = 0;
            float3 alignment = float3.zero, cohesion = float3.zero, separation = float3.zero;
            for (int i = 0; i < neighborOffsets.Length; i++)
            {
                int3 cellBoidCurrentPosition = (int3)math.floor(localTransform.ValueRO.Position / boidHashing.cellSize) + neighborOffsets[i];
                if (boidHashing.mapBoid.TryGetFirstValue(cellBoidCurrentPosition, out Entity neighborEntity, out NativeParallelMultiHashMapIterator<int3> iterator))
                {
                    do
                    {
                        if (neighborEntity == currentBoidEntity) continue;
                        RefRO<LocalTransform> neighborTransform = SystemAPI.GetComponentRO<LocalTransform>(neighborEntity);
                        float3 neighborPosition = neighborTransform.ValueRO.Position;
                        float distance = math.distance(localTransform.ValueRO.Position, neighborPosition);
                        if (distance <= boid.ValueRO.neighborDistance && distance > 0)
                        {
                            float3 neighborDirection = neighborTransform.ValueRO.Forward();
                            alignment += neighborDirection * boid.ValueRO.alignmentWeight;
                            cohesion += neighborPosition * boid.ValueRO.cohesionWeight;
                            if (distance < boid.ValueRO.separationDistance)
                            {
                                float3 separationDirection = (localTransform.ValueRO.Position - neighborPosition) / distance;
                                separation += separationDirection * boid.ValueRO.separationWeight;
                            }
                            neighborCount++;
                        }
                    } while (boidHashing.mapBoid.TryGetNextValue(out neighborEntity, ref iterator));
                }
            }
            float3 finalDir = float3.zero;
            if (neighborCount > 0)
            {
                alignment = math.normalizesafe(alignment / neighborCount) * boid.ValueRO.alignmentWeight;
                cohesion = math.normalizesafe((cohesion / neighborCount - localTransform.ValueRO.Position)) * boid.ValueRO.cohesionWeight;
                separation = math.normalizesafe(separation) * boid.ValueRO.separationWeight;
                float3 desired = alignment + cohesion + separation;
                finalDir = math.normalizesafe(math.lerp(finalDir, desired, SystemAPI.Time.DeltaTime * boid.ValueRO.rotationSpeed));
            }
            boid.ValueRW.direction = finalDir;
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        neighborOffsets.Dispose();
    }
    private void GetNeighborOffSet()
    {
        neighborOffsets = new NativeArray<int3>(27, Allocator.Persistent);
        neighborOffsets[0] = new int3(-1, -1, -1);
        neighborOffsets[1] = new int3(-1, -1, 0);
        neighborOffsets[2] = new int3(-1, -1, 1);
        neighborOffsets[3] = new int3(-1, 0, -1);
        neighborOffsets[4] = new int3(-1, 0, 0);
        neighborOffsets[5] = new int3(-1, 0, 1);
        neighborOffsets[6] = new int3(-1, 1, -1);
        neighborOffsets[7] = new int3(-1, 1, 0);
        neighborOffsets[8] = new int3(-1, 1, 1);
        neighborOffsets[9] = new int3(0, -1, -1);
        neighborOffsets[10] = new int3(0, -1, 0);
        neighborOffsets[11] = new int3(0, -1, 1);
        neighborOffsets[12] = new int3(0, 0, -1);
        neighborOffsets[13] = new int3(0, 0, 0);
        neighborOffsets[14] = new int3(0, 0, 1);
        neighborOffsets[15] = new int3(0, 1, -1);
        neighborOffsets[16] = new int3(0, 1, 0);
        neighborOffsets[17] = new int3(0, 1, 1);
        neighborOffsets[18] = new int3(1, -1, -1);
        neighborOffsets[19] = new int3(1, -1, 0);
        neighborOffsets[20] = new int3(1, -1, 1);
        neighborOffsets[21] = new int3(1, 0, -1);
        neighborOffsets[22] = new int3(1, 0, 0);
        neighborOffsets[23] = new int3(1, 0, 1);
        neighborOffsets[24] = new int3(1, 1, -1);
        neighborOffsets[25] = new int3(1, 1, 0);
        neighborOffsets[26] = new int3(1, 1, 1);
    }
}
