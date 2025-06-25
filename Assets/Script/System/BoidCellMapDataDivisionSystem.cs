using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static BoidCellMapUpdateDataSystem;
using static Enum;
[UpdateAfter(typeof(BoidCellMapUpdateDataSystem))]
partial struct BoidCellMapDataDivisionSystem : ISystem
{
    private Entity simpleEntity;
    private Entity incrementalEntity;
    private Entity balancedEntity;
    public struct SimpleKDTreeMap : IComponentData
    {

    }
    public struct IncrementalKDTreeMap : IComponentData
    {

    }
    public struct BalancedKDTreeMap : IComponentData
    {

    }
    public struct KDTreeMapContainer : IComponentData
    {
        public NativeHashMap<int3, CellDataMap> map;
        public int count => map.Count;
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        Allocator allocator = Allocator.Persistent;
        simpleEntity = state.EntityManager.CreateEntity();
        incrementalEntity = state.EntityManager.CreateEntity();
        balancedEntity = state.EntityManager.CreateEntity();

        state.EntityManager.AddComponentData(simpleEntity, new KDTreeMapContainer
        {
            map = new NativeHashMap<int3, CellDataMap>(10000, allocator)
        });
        state.EntityManager.AddComponentData(incrementalEntity, new KDTreeMapContainer
        {
            map = new NativeHashMap<int3,   CellDataMap>(10000, allocator)
        });
        state.EntityManager.AddComponentData(balancedEntity, new KDTreeMapContainer
        {
            map = new NativeHashMap<int3, CellDataMap>(10000, allocator)
        });

        state.EntityManager.AddComponentData(simpleEntity, new SimpleKDTreeMap
        {

        });
        state.EntityManager.AddComponentData(incrementalEntity, new IncrementalKDTreeMap
        {

        });
        state.EntityManager.AddComponentData(balancedEntity, new BalancedKDTreeMap
        {

        });
        state.EntityManager.SetName(simpleEntity, "SimpleKDTreeMap");
        state.EntityManager.SetName(incrementalEntity, "IncrementalKDTreeMap");
        state.EntityManager.SetName(balancedEntity, "BalancedKDTreeMap");
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.RequireForUpdate<BoidCellMapUpdateTotal>();
        BoidCellMapUpdateTotal total = SystemAPI.GetSingleton<BoidCellMapUpdateTotal>();
        NativeHashMap<int3, CellDataMap> map = total.mapCellDatasTotal;
        NativeHashMap<int3, CellDataMap> simpleMap = SystemAPI.GetComponentRW<KDTreeMapContainer>(simpleEntity).ValueRW.map;
        NativeHashMap<int3, CellDataMap> incrementalMap = SystemAPI.GetComponentRW<KDTreeMapContainer>(incrementalEntity).ValueRW.map;
        NativeHashMap<int3, CellDataMap> balancedMap = SystemAPI.GetComponentRW<KDTreeMapContainer>(balancedEntity).ValueRW.map;

        simpleMap.Clear();
        incrementalMap.Clear();
        balancedMap.Clear();
        var keys = map.GetKeyArray(Allocator.Temp);
        foreach (var key in keys)
        {
            var cell = map[key];
            KDTreeType type = EvaluateKDTreeType(cell.curentBoidCount, cell.previousBoidCount);
            switch (type)
            {
                case KDTreeType.Simple:
                    simpleMap.Add(key, cell);
                    break;
                case KDTreeType.Incremental:
                    incrementalMap.Add(key, cell);
                    break;
                case KDTreeType.Balanced:
                    balancedMap.Add(key, cell);
                    break;
            }
        }
        keys.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        DisposeMap(simpleEntity, ref state);
        DisposeMap(incrementalEntity, ref state);
        DisposeMap(balancedEntity, ref state);
    }
    private void DisposeMap(Entity e, ref SystemState state)
    {
        if (SystemAPI.HasComponent<KDTreeMapContainer>(e))
        {
            var map = SystemAPI.GetComponent<KDTreeMapContainer>(e).map;
            foreach (var kv in map)
            {
                var v = kv.Value;
                if (v.currentBoidPositions.IsCreated) v.currentBoidPositions.Dispose();
                if (v.previousBoidPositions.IsCreated) v.previousBoidPositions.Dispose();
                if (v.nodes.IsCreated) v.nodes.Dispose();
            }
            map.Dispose();
        }
    }
    private KDTreeType EvaluateKDTreeType(int currentCount, int lastCount)
    {
        float changeRatio = math.abs(currentCount - lastCount) / math.max(1f, lastCount);

        if (changeRatio < 0.01f)
            return KDTreeType.Simple;

        if (currentCount >= 100 && currentCount <= 300 && changeRatio < 0.3f)
            return KDTreeType.Incremental;

        return KDTreeType.Balanced;
    }
}
