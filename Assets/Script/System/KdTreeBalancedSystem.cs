using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static BoidCellMapDataDivisionSystem;
using static BoidCellMapUpdateDataSystem;
[UpdateBefore(typeof(BoidCellMapDataDivisionSystem))]
partial struct KdTreeBalancedSystem : ISystem
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
        RefRW<BoidCellMapUpdateTotal> boidCellMapUpdateTotal = SystemAPI.GetSingletonRW<BoidCellMapUpdateTotal>();
        foreach ((RefRW<KDTreeMapContainer> KDTreeMapContainer, RefRO<BalancedKDTreeMap> balancedKDTreeMap) in SystemAPI.Query<RefRW<KDTreeMapContainer>, RefRO<BalancedKDTreeMap>>())
        {
            NativeHashMap<int3, CellDataMap> mapCellDatas = KDTreeMapContainer.ValueRO.map;
            if (mapCellDatas.IsEmpty)
            {
                return; // If the map is empty, no need to proceed
            }
            foreach (KVPair<int3, CellDataMap> mapCellData in mapCellDatas)
            {
                // Initialize the KDTree for the cell 
                kdTreeTool.BuildTree(mapCellData.Value.currentBoidPositions, Allocator.Temp);
                mapCellData.Value.nodes = kdTreeTool.GetNodes(Allocator.Temp); // Assign the nodes to the cell data
                foreach (KdTreeNode node in mapCellData.Value.nodes)
                {
                    boidCellMapUpdateTotal.ValueRW.mapCellDatasNodeBoidPositionsTotal.Add(mapCellData.Key, node);
                }
                mapCellData.Value.hasKDTree = true; // Mark that the KDTree has been built for this cell
                kdTreeTool.DisposeNode(); // DisposeNode of the KDTree after use
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
