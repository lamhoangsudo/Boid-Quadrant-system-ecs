using OOP.Boid;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static UnityEditor.Searcher.SearcherWindow;

[UpdateAfter(typeof(BoidHashingSystem))]
partial struct BoidCellMapUpdateSystem : ISystem
{
    private Entity boidCellMapEntity;
    public struct BoidCellMapUpdate : IComponentData
    {
        public NativeHashMap<int3, CellDataMap> mapCellData;
    }
    public struct CellDataMap
    {
        public NativeArray<float3> boidPositions;
        public int curentBoidCount;
        public int lastBoidCount;
        public bool isDirty;
        public Enum.KDTreeType kdTreeType;
        public bool hasKDTree;
    }
    private NativeParallelMultiHashMap<int3, Entity> mapBoid;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BoidHashingSystem.BoidHashing>();
        boidCellMapEntity = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponentData(boidCellMapEntity, new BoidCellMapUpdate
        {
            mapCellData = new NativeHashMap<int3, CellDataMap>(10000, Allocator.Persistent)
        });
        state.EntityManager.SetName(boidCellMapEntity, "BoidCellMapUpdate");
        mapBoid = new(10000, Allocator.Persistent);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Ensure that BoidHashingSystem has run before this system
        RefRW<BoidCellMapUpdate> boidCellMapUpdate = SystemAPI.GetComponentRW<BoidCellMapUpdate>(boidCellMapEntity);
        // Clear the mapCellData for the next update
        boidCellMapUpdate.ValueRW.mapCellData.Clear();
        NativeArray<int3> keyArray = SystemAPI.GetSingleton<BoidHashingSystem.BoidHashing>().mapBoid.GetKeyArray(Allocator.Temp);
        NativeList<float3> allBoidPositionsInCell = new(Allocator.Temp);
        // Iterate through each key in the mapBoid and collect boid positions
        foreach (int3 key in keyArray)
        {
            // Initialize the list for this cell
            allBoidPositionsInCell.Clear();
            // Get all boid entities in the current cell
            foreach (Entity boidEntity in SystemAPI.GetSingleton<BoidHashingSystem.BoidHashing>().mapBoid.GetValuesForKey(key))
            {
                RefRO<LocalTransform> boidLocalTransform = SystemAPI.GetComponentRO<LocalTransform>(boidEntity);
                float3 boidPosition = boidLocalTransform.ValueRO.Position;
                allBoidPositionsInCell.Add(boidPosition);
            }
            // Create a NativeArray from the collected positions
            NativeArray<float3> allBoidPositionsInCellArray = new(allBoidPositionsInCell.Length, Allocator.Persistent);
            NativeArray<float3>.Copy(allBoidPositionsInCell.AsArray(), allBoidPositionsInCellArray);
            // Create a new CellDataMap and populate it with the boid positions and counts
            boidCellMapUpdate.ValueRW.mapCellData[key] = new CellDataMap
            {
                boidPositions = allBoidPositionsInCellArray,
                curentBoidCount = allBoidPositionsInCell.Length,
                lastBoidCount = allBoidPositionsInCell.Length,
                isDirty = false,
                kdTreeType = Enum.KDTreeType.None,
                hasKDTree = false
            };
        }
        // Dispose of the temporary arrays
        keyArray.Dispose();
        allBoidPositionsInCell.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        if (SystemAPI.HasSingleton<BoidCellMapUpdate>())
        {
            BoidCellMapUpdate boidCellMapUpdate = SystemAPI.GetSingleton<BoidCellMapUpdate>();
            foreach (KVPair<int3, CellDataMap> cellData in boidCellMapUpdate.mapCellData)
            {
                if (cellData.Value.boidPositions.IsCreated)
                {
                    cellData.Value.boidPositions.Dispose();
                }
            }
            boidCellMapUpdate.mapCellData.Dispose();
            state.EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<BoidCellMapUpdate>());
        }
    }
}
