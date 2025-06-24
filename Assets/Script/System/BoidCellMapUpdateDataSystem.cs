using OOP.Boid;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static Enum;

[UpdateAfter(typeof(BoidHashingSystem))]
partial struct BoidCellMapUpdateDataSystem : ISystem
{
    private const float allowedChangeCount = 4f;
    private Entity boidCellMapUpdateTotalEntity;
    public struct BoidCellMapUpdateTotal : IComponentData
    {
        public NativeHashMap<int3, CellDataMap> mapCellDatasTotal;
    }
    public struct CellDataMap
    {
        public NativeArray<float3> currentBoidPositions; // Positions of boids in the cell
        public NativeArray<float3> previousBoidPositions; // Previous positions of boids in the cell
        public NativeArray<KdTreeNode> nodes; // Nodes for KD-Tree
        public int curentBoidCount;
        public int previousBoidCount;
        public bool hasKDTree;
    }
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BoidHashingSystem.BoidHashing>();
        boidCellMapUpdateTotalEntity = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponentData(boidCellMapUpdateTotalEntity, new BoidCellMapUpdateTotal
        {
            mapCellDatasTotal = new NativeHashMap<int3, CellDataMap>(30000, Allocator.Persistent),
        });
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Initialize the mapCellDatasTotal for the next update
        NativeArray<int3> keyArray = SystemAPI.GetSingleton<BoidHashingSystem.BoidHashing>().mapBoid.GetKeyArray(Allocator.Temp);
        RefRW<BoidCellMapUpdateTotal> boidCellMapUpdateTotal = SystemAPI.GetComponentRW<BoidCellMapUpdateTotal>(boidCellMapUpdateTotalEntity);
        NativeHashMap<int3, CellDataMap> mapCellDatasTotal = boidCellMapUpdateTotal.ValueRO.mapCellDatasTotal;
        NativeHashSet<int3> uniqueKeys = new(keyArray.Length, Allocator.Temp);
        NativeParallelMultiHashMap<int3, Entity> mapBoid = SystemAPI.GetSingleton<BoidHashingSystem.BoidHashing>().mapBoid;
        foreach (int3 key in keyArray)
        {
            uniqueKeys.Add(key); // Add uniqueKeys to the hash set for quick lookup
        }
        foreach (int3 key in uniqueKeys)
        {
            NativeList<float3> allCurrentBoidPositionsInCell = new(Allocator.Temp);
            foreach (Entity boidEntity in mapBoid.GetValuesForKey(key))
            {
                RefRO<LocalTransform> boidLocalTransform = SystemAPI.GetComponentRO<LocalTransform>(boidEntity);
                allCurrentBoidPositionsInCell.Add(boidLocalTransform.ValueRO.Position);
            }

            var currentCount = allCurrentBoidPositionsInCell.Length;
            var currentArray = new NativeArray<float3>(currentCount, Allocator.Persistent);
            NativeArray<float3>.Copy(allCurrentBoidPositionsInCell.AsArray(), currentArray);

            if (mapCellDatasTotal.TryGetValue(key, out var cellData))
            {
                if (cellData.previousBoidPositions.IsCreated)
                    cellData.previousBoidPositions.Dispose();

                cellData.previousBoidPositions = cellData.currentBoidPositions;
                cellData.currentBoidPositions = currentArray;
                cellData.previousBoidCount = cellData.curentBoidCount;
                cellData.curentBoidCount = currentCount;

                mapCellDatasTotal[key] = cellData;
            }
            else
            {
                var newCell = new CellDataMap
                {
                    currentBoidPositions = currentArray,
                    previousBoidPositions = new NativeArray<float3>(0, Allocator.Persistent),
                    nodes = new NativeArray<KdTreeNode>(0, Allocator.Persistent),
                    curentBoidCount = currentCount,
                    previousBoidCount = 0,
                    hasKDTree = false
                };
                mapCellDatasTotal.Add(key, newCell);
            }
            allCurrentBoidPositionsInCell.Dispose();
        }
        NativeList<int3> keysToRemove = new(Allocator.Temp);
        var keys = mapCellDatasTotal.GetKeyArray(Allocator.Temp);
        foreach (var key in keys)
        {
            if (!uniqueKeys.Contains(key))
            {
                var data = mapCellDatasTotal[key];
                if (data.currentBoidPositions.IsCreated) data.currentBoidPositions.Dispose();
                if (data.previousBoidPositions.IsCreated) data.previousBoidPositions.Dispose();
                if (data.nodes.IsCreated) data.nodes.Dispose();
                keysToRemove.Add(key);
            }
        }
        foreach (var key in keysToRemove)
        {
            mapCellDatasTotal.Remove(key);
        }

        keys.Dispose();
        keysToRemove.Dispose();
        keyArray.Dispose();
        uniqueKeys.Dispose();
        mapBoid.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        if (SystemAPI.HasComponent<BoidCellMapUpdateTotal>(boidCellMapUpdateTotalEntity))
        {
            var map = SystemAPI.GetComponent<BoidCellMapUpdateTotal>(boidCellMapUpdateTotalEntity).mapCellDatasTotal;
            foreach (var kvp in map)
            {
                if (kvp.Value.currentBoidPositions.IsCreated) kvp.Value.currentBoidPositions.Dispose();
                if (kvp.Value.previousBoidPositions.IsCreated) kvp.Value.previousBoidPositions.Dispose();
                if (kvp.Value.nodes.IsCreated) kvp.Value.nodes.Dispose();
            }
            map.Dispose();
        }
    }
}
