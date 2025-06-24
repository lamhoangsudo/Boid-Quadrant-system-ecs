using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static BoidCellMapUpdateDataSystem;

partial struct KdTreeBuildSystem : ISystem
{
    private KdTreeTool kdTreeSimple;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        kdTreeSimple = new KdTreeTool();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //foreach(RefRW<BoidCellMapUpdateSystem.BoidCellMapUpdateTotal> boidCellMapUpdate in SystemAPI.Query<RefRW<BoidCellMapUpdateSystem.BoidCellMapUpdateTotal>>())
        //{
        //    NativeHashMap<int3, CellDataMap> mapCellDatasTotal = boidCellMapUpdate.ValueRO.mapCellDatasTotal;
        //    foreach(KVPair<int3, CellDataMap> mapCellData in mapCellDatasTotal)
        //    {
        //        if(!mapCellData.Value.hasKDTree)
        //        {
        //            // Initialize the KDTree for the cell 
        //            kdTreeSimple.BuildTree(mapCellData.Value.currentBoidPositions, Allocator.Temp);
        //            mapCellData.Value.nodes = kdTreeSimple.GetNodes(Allocator.Temp); // Assign the nodes to the cell data
        //            mapCellData.Value.hasKDTree = true; // Mark that the KDTree has been built for this cell
        //            kdTreeSimple.DisposeNode(); // DisposeNode of the KDTree after use
        //        }
        //    }
        //    mapCellDatasTotal.Dispose(); // DisposeNode of the mapCellDatasTotal after processing
        //}
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
