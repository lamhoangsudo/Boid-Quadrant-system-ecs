using OOP.Boid;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static Enum;

[UpdateAfter(typeof(BoidHashingSystem))]
partial struct BoidCellMapUpdateSystem : ISystem
{
    private const float allowedChangeCount = 4f;
    private Entity boidKdTreeSimpleCellMapEntity;
    private Entity boidKdTreeIncrementalCellMapEntity;
    private Entity boidKdTreeBalancedCellMapEntity;
    public struct KdTreeSimpleTag : IComponentData
    {
        public int length; // Length of the KD-Tree nodes array, can be used for debugging or other purposes
    }
    public struct KdTreeIncrementalTag : IComponentData
    {
        public int length; // Length of the KD-Tree nodes array, can be used for debugging or other purposes
    }
    public struct KdTreeBalancedTag : IComponentData
    {
        public int length; // Length of the KD-Tree nodes array, can be used for debugging or other purposes
    }
    public struct BoidCellMapUpdate : IComponentData
    {
        public NativeHashMap<int3, CellDataMap> mapCellDatas;
        public Enum.KDTreeType kdTreeType;
    }
    public struct CellDataMap
    {
        public NativeArray<float3> boidPositions; // Positions of boids in the cell
        public NativeArray<KdTreeNode> nodes; // Nodes for KD-Tree
        public int curentBoidCount;
        public int lastBoidCount;
        public bool hasKDTree;
    }
    private NativeParallelMultiHashMap<int3, Entity> mapBoid;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BoidHashingSystem.BoidHashing>();
        boidKdTreeSimpleCellMapEntity = state.EntityManager.CreateEntity();
        boidKdTreeIncrementalCellMapEntity = state.EntityManager.CreateEntity();
        boidKdTreeBalancedCellMapEntity = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponentData(boidKdTreeSimpleCellMapEntity, new BoidCellMapUpdate
        {
            mapCellDatas = new NativeHashMap<int3, CellDataMap>(10000, Allocator.Persistent),
            kdTreeType = Enum.KDTreeType.Simple
        });
        state.EntityManager.AddComponent<KdTreeSimpleTag>(boidKdTreeSimpleCellMapEntity);
        state.EntityManager.AddComponentData(boidKdTreeIncrementalCellMapEntity, new BoidCellMapUpdate
        {
            mapCellDatas = new NativeHashMap<int3, CellDataMap>(10000, Allocator.Persistent),
            kdTreeType = Enum.KDTreeType.Incremental
        });
        state.EntityManager.AddComponent<KdTreeIncrementalTag>(boidKdTreeIncrementalCellMapEntity);
        state.EntityManager.AddComponentData(boidKdTreeBalancedCellMapEntity, new BoidCellMapUpdate
        {
            mapCellDatas = new NativeHashMap<int3, CellDataMap>(10000, Allocator.Persistent),
            kdTreeType = Enum.KDTreeType.Balanced
        });
        state.EntityManager.AddComponent<KdTreeBalancedTag>(boidKdTreeBalancedCellMapEntity);
        state.EntityManager.SetName(boidKdTreeSimpleCellMapEntity, "boidKdTreeSimpleCellMapEntity");
        state.EntityManager.SetName(boidKdTreeIncrementalCellMapEntity, "boidKdTreeIncrementalCellMapEntity");
        state.EntityManager.SetName(boidKdTreeBalancedCellMapEntity, "boidKdTreeBalancedCellMapEntity");
        mapBoid = new(10000, Allocator.Persistent);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Initialize the mapCellDatas for the next update
        NativeArray<int3> keyArray = SystemAPI.GetSingleton<BoidHashingSystem.BoidHashing>().mapBoid.GetKeyArray(Allocator.Temp);
        NativeHashSet<int3> uniqueKeys = new(keyArray.Length, Allocator.Temp);
        foreach (int3 key in keyArray)
        {
            uniqueKeys.Add(key); // Add uniqueKeys to the hash set for quick lookup
        }
        NativeList<float3> allBoidPositionsInCell = new(Allocator.Temp);
        foreach (RefRW<BoidCellMapUpdate> boidCellMapUpdate in SystemAPI.Query<RefRW<BoidCellMapUpdate>>())
        {
            // Initialize the mapCellDatas for the next update
            Enum.KDTreeType newKdTreeType = Enum.KDTreeType.None;
            int currentBoidCount = 0;
            int lastBoidCount = 0;
            foreach (int3 key in uniqueKeys)
            {
                allBoidPositionsInCell.Clear(); // Clear the NativeList for the next key
                // Get all boid entities in the current cell
                foreach (Entity boidEntity in SystemAPI.GetSingleton<BoidHashingSystem.BoidHashing>().mapBoid.GetValuesForKey(key))
                {
                    RefRO<LocalTransform> boidLocalTransform = SystemAPI.GetComponentRO<LocalTransform>(boidEntity);
                    float3 boidPosition = boidLocalTransform.ValueRO.Position;
                    allBoidPositionsInCell.Add(boidPosition);
                }
                // Create a NativeArray from the collected positions
                NativeArray<float3> allBoidPositionsInCellArray = new(allBoidPositionsInCell.Length, Allocator.Temp);
                NativeArray<float3>.Copy(allBoidPositionsInCell.AsArray(), allBoidPositionsInCellArray);
                // Determine the KDTreeType based on the current and last boid counts
                if (boidCellMapUpdate.ValueRO.mapCellDatas.ContainsKey(key))
                {
                    if (allBoidPositionsInCellArray.Length == 0)
                    {
                        boidCellMapUpdate.ValueRW.mapCellDatas.Remove(key); // Remove the cell if no boids are present
                        continue; // Skip further processing for this key
                    }
                    // If the cell already exists, retrieve the current and last boid counts
                    currentBoidCount = allBoidPositionsInCellArray.Length;
                    lastBoidCount = boidCellMapUpdate.ValueRO.mapCellDatas[key].curentBoidCount;
                    float deltaCount = (float)math.abs(currentBoidCount - lastBoidCount);
                    float deltaCountPercent = deltaCount / math.max(1, lastBoidCount);
                    // Determine the KDTreeType based on the change in boid count
                    if (deltaCountPercent <= 0.01f)
                    {
                        newKdTreeType = Enum.KDTreeType.Simple;
                    }
                    else if (deltaCountPercent <= 0.3 && deltaCountPercent > 0.01f && currentBoidCount >= 100f && currentBoidCount <= 300f)
                    {
                        newKdTreeType = Enum.KDTreeType.Incremental;
                    }
                    else
                    {
                        newKdTreeType = Enum.KDTreeType.Balanced;
                    }
                    if (boidCellMapUpdate.ValueRO.kdTreeType != newKdTreeType)
                    {
                        boidCellMapUpdate.ValueRW.mapCellDatas.Remove(key); // Remove the old cell data if the KDTreeType has changed
                    }
                    else
                    {
                        boidCellMapUpdate.ValueRW.mapCellDatas[key] = new CellDataMap
                        {
                            boidPositions = allBoidPositionsInCellArray,
                            curentBoidCount = currentBoidCount,
                            lastBoidCount = lastBoidCount,
                            hasKDTree = false // Set to false initially, will be updated later
                        };
                    }
                }
                else
                {
                    // This means the cell is being created for the first time
                    currentBoidCount = allBoidPositionsInCellArray.Length;
                    lastBoidCount = 0; // No previous count
                    // If this is the first time the cell is being created, set the KDTreeType based on the current boid count
                    if (currentBoidCount < 30)
                        newKdTreeType = KDTreeType.Simple;
                    else if (currentBoidCount <= 300)
                        newKdTreeType = KDTreeType.Incremental;
                    else
                        newKdTreeType = KDTreeType.Balanced;
                    if (newKdTreeType == boidCellMapUpdate.ValueRO.kdTreeType)
                    {
                        boidCellMapUpdate.ValueRW.mapCellDatas[key] = new CellDataMap
                        {
                            boidPositions = allBoidPositionsInCellArray,
                            curentBoidCount = currentBoidCount,
                            lastBoidCount = currentBoidCount,
                            hasKDTree = false
                        };
                    }
                }
                allBoidPositionsInCellArray.Dispose(); // Dispose of the NativeArray after use
            }
        }
        keyArray.Dispose(); // Dispose of the key array after use
        uniqueKeys.Dispose(); // Dispose of the NativeHashSet after use
        allBoidPositionsInCell.Dispose(); // Dispose of the NativeList after use
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        // Dispose of the NativeHashMap and other resources
        foreach (RefRW<BoidCellMapUpdate> boidCellMapUpdate in SystemAPI.Query<RefRW<BoidCellMapUpdate>>())
        {
            boidCellMapUpdate.ValueRW.mapCellDatas.Dispose();
        }
        mapBoid.Dispose(); // Dispose of the mapBoid NativeParallelMultiHashMap
        state.EntityManager.DestroyEntity(boidKdTreeSimpleCellMapEntity);
        state.EntityManager.DestroyEntity(boidKdTreeIncrementalCellMapEntity);
        state.EntityManager.DestroyEntity(boidKdTreeBalancedCellMapEntity);
    }
}
