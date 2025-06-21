using OOP.Boid;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateAfter(typeof(BoidHashingSystem))]
partial struct BoidCellMapUpdateSystem : ISystem
{
    private const float allowedChangeCount = 4f;
    private Entity boidCellMapEntity;
    public struct BoidCellMapUpdate : IComponentData
    {
        public NativeHashMap<int3, CellDataMap> mapCellDatas;
    }
    public struct CellDataMap
    {
        public NativeArray<float3> boidPositions;
        public int curentBoidCount;
        public int lastBoidCount;
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
            mapCellDatas = new NativeHashMap<int3, CellDataMap>(10000, Allocator.Persistent)
        });
        state.EntityManager.SetName(boidCellMapEntity, "BoidCellMapUpdate");
        mapBoid = new(10000, Allocator.Persistent);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Ensure that BoidHashingSystem has run before this system
        RefRW<BoidCellMapUpdate> boidCellMapUpdate = SystemAPI.GetComponentRW<BoidCellMapUpdate>(boidCellMapEntity);
        // Clear the mapCellDatas for the next update
        boidCellMapUpdate.ValueRW.mapCellDatas.Clear();
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
            // Determine the KDTreeType based on the current and last boid counts
            Enum.KDTreeType kdTreeType = Enum.KDTreeType.None;
            int currentBoidCount = 0;
            int lastBoidCount = 0;
            if (boidCellMapUpdate.ValueRO.mapCellDatas.ContainsKey(key))
            {
                // If the cell already exists, retrieve the current and last boid counts
                currentBoidCount = allBoidPositionsInCellArray.Length;
                lastBoidCount = boidCellMapUpdate.ValueRO.mapCellDatas[key].curentBoidCount;
                // Determine the KDTreeType based on the change in boid count
                float deltaCount = (float)math.abs(currentBoidCount - lastBoidCount);
                float deltaCountPercent = deltaCount / math.max(1, lastBoidCount);
                if(deltaCountPercent <= 0.01f)
                {
                    kdTreeType = Enum.KDTreeType.Simple;
                }
                else if (deltaCountPercent <= 0.3 && deltaCountPercent > 0.01f && currentBoidCount >= 100f && currentBoidCount <= 300f)
                {
                    kdTreeType = Enum.KDTreeType.Incremental;
                }
                else
                {
                    kdTreeType = Enum.KDTreeType.Balanced;
                }
            }
            else
            {
                // If the cell does not exist, set the current and last boid counts to the length of the array
                currentBoidCount = allBoidPositionsInCellArray.Length;
                lastBoidCount = allBoidPositionsInCellArray.Length;
                kdTreeType = Enum.KDTreeType.None; // Default type for new cells
            }
            // Create a new CellDataMap and populate it with the boid positions and counts
            boidCellMapUpdate.ValueRW.mapCellDatas[key] = new CellDataMap
            {
                boidPositions = allBoidPositionsInCellArray,
                curentBoidCount = currentBoidCount,
                lastBoidCount = lastBoidCount,
                kdTreeType = kdTreeType,
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
            foreach (KVPair<int3, CellDataMap> cellData in boidCellMapUpdate.mapCellDatas)
            {
                if (cellData.Value.boidPositions.IsCreated)
                {
                    cellData.Value.boidPositions.Dispose();
                }
            }
            boidCellMapUpdate.mapCellDatas.Dispose();
            state.EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<BoidCellMapUpdate>());
        }
    }
}
