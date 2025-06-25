using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static BoidCellMapUpdateDataSystem;
[UpdateBefore(typeof(KdTreeSimpleSystem))]
[UpdateBefore(typeof(KdTreeBalancedSystem))]
[UpdateBefore(typeof(KdTreeIncrementaSystem))]
partial struct BoidCaculaterDirectionSystem : ISystem
{
    private KdTreeTool kdTreeTool;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        kdTreeTool = new KdTreeTool();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        BoidHashingSystem.BoidHashing boidHashing = SystemAPI.GetSingleton<BoidHashingSystem.BoidHashing>();
        NativeParallelMultiHashMap<int3, KdTreeNode> mapCellNodeDatasTotal = SystemAPI.GetSingleton<BoidCellMapUpdateTotal>().mapCellDatasNodeBoidPositionsTotal;
        /*
        foreach ((RefRO<LocalTransform> localTransform, RefRW<Boid> boid, Entity currentBoidEntity) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<Boid>>().WithEntityAccess())
        {
            int neighborCount = 0;
            float3 alignment = float3.zero, cohesion = float3.zero, separation = float3.zero;
            for (int i = 0; i < neighborOffsets.Length; i++)
            {
                int3 cellBoidCurrentPosition = (int3)math.floor(localTransform.ValueRO.Position / boidHashing.cellSize) + neighborOffsets[i];
                if (boidHashing.mapBoid.TryGetFirstValue(cellBoidCurrentPosition, out Entity neighborEntity, out NativeParallelMultiHashMapIterator<int3> iteratorNode))
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
                            alignment += neighborDirection;
                            cohesion += neighborPosition;
                            if (distance < boid.ValueRO.separationDistance)
                            {
                                float3 separationDirection = (localTransform.ValueRO.Position - neighborPosition) / distance;
                                separation += separationDirection;
                            }
                            neighborCount++;
                        }
                    } while (boidHashing.mapBoid.TryGetNextValue(out neighborEntity, ref iteratorNode));
                }
            }
            float3 finalDir = float3.zero;
            if (neighborCount > 0)
            {
                alignment = math.normalizesafe(alignment) * boid.ValueRO.alignmentWeight;
                cohesion = math.normalizesafe((cohesion / neighborCount - localTransform.ValueRO.Position)) * boid.ValueRO.cohesionWeight;
                separation = math.normalizesafe(separation) * boid.ValueRO.separationWeight;
                float3 desired = alignment + cohesion + separation;
                finalDir = math.normalizesafe(math.lerp(finalDir, desired, boid.ValueRO.smoothFactor * SystemAPI.Time.DeltaTime));
            }
            boid.ValueRW.alignment = alignment;
            boid.ValueRW.cohesion = cohesion;
            boid.ValueRW.separation = separation;
            boid.ValueRW.direction = finalDir;
            */
        BoidCaculaterDirectionJob boidCaculaterDirectionJob = new BoidCaculaterDirectionJob
        {
            cellSize = boidHashing.cellSize,
            deltaTime = SystemAPI.Time.DeltaTime,
            mapBoid = boidHashing.mapBoid,
            kdTreeTool = kdTreeTool,
            mapCellNodeDatasTotal = mapCellNodeDatasTotal,
            neighborTransformLookUp = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: true)
        };
        boidCaculaterDirectionJob.ScheduleParallel();
    }


    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
    [BurstCompile]
    public partial struct BoidCaculaterDirectionJob : IJobEntity
    {
        public float cellSize;
        public float deltaTime;
        [ReadOnly] public NativeParallelMultiHashMap<int3, Entity> mapBoid;
        [ReadOnly] public ComponentLookup<LocalTransform> neighborTransformLookUp;
        [ReadOnly] public KdTreeTool kdTreeTool;
        [ReadOnly] public NativeParallelMultiHashMap<int3, KdTreeNode> mapCellNodeDatasTotal;
        public void Execute(in LocalTransform localTransform, ref Boid boid, Entity currentBoidEntity)
        {
            boid.changeDirectionTime -= deltaTime;
            if (boid.changeDirectionTime > 0) return;
            boid.changeDirectionTime = boid.changeDirectionTimeMax;
            int neighborCount = 0;
            float3 alignment = float3.zero, cohesion = float3.zero, separation = float3.zero;
            int3 cellBoidCurrentPosition = (int3)math.floor(localTransform.Position / cellSize);
            kdTreeTool.nodes.Clear();
            //if (mapCellNodeDatasTotal.TryGetFirstValue(cellBoidCurrentPosition, out KdTreeNode node, out var iteratorNode))
            //{
            //    do
            //    {
            //        kdTreeTool.nodes.Add(node);
            //    }
            //    while (mapCellNodeDatasTotal.TryGetNextValue(out node, ref iteratorNode));
            //}
            NativeList<float3> neighborPositions = kdTreeTool.Search(localTransform.Position, boid.neighborDistance, Allocator.TempJob);
            if (mapBoid.TryGetFirstValue(cellBoidCurrentPosition, out Entity neighborEntity, out NativeParallelMultiHashMapIterator<int3> iterator))
            {
                do
                {
                    if (neighborEntity == currentBoidEntity) continue;
                    RefRO<LocalTransform> neighborTransform = neighborTransformLookUp.GetRefRO(neighborEntity);
                    float3 neighborPosition = neighborTransform.ValueRO.Position;
                    if (neighborPositions.Contains(neighborPosition) == false)
                    {
                        continue; // Skip if the neighbor position is not in the kdTree search results
                    }
                    float distance = math.distance(localTransform.Position, neighborPosition);
                    if (distance <= boid.neighborDistance && distance > 0)
                    {
                        float3 neighborDirection = neighborTransform.ValueRO.Forward();
                        alignment += neighborDirection;
                        cohesion += neighborPosition;
                        if (distance < boid.separationDistance)
                        {
                            float3 separationDirection = (localTransform.Position - neighborPosition) / distance;
                            separation += separationDirection;
                        }
                        neighborCount++;
                    }
                } while (mapBoid.TryGetNextValue(out neighborEntity, ref iterator));
            }
            float3 finalDir = float3.zero;
            if (neighborCount > 0)
            {
                alignment = math.normalizesafe(alignment) * boid.alignmentWeight;
                cohesion = math.normalizesafe((cohesion / neighborCount - localTransform.Position)) * boid.cohesionWeight;
                separation = math.normalizesafe(separation) * boid.separationWeight;
                float3 desired = alignment + cohesion + separation;
                finalDir = math.normalizesafe(math.lerp(finalDir, desired, boid.smoothFactor * deltaTime));
            }
            boid.alignment = alignment;
            boid.cohesion = cohesion;
            boid.separation = separation;
            boid.direction = finalDir;
        }
    }
}