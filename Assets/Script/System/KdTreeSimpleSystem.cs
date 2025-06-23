using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static BoidCellMapUpdateSystem;
[UpdateAfter(typeof(BoidCellMapUpdateSystem))]
partial struct KdTreeSimpleSystem : ISystem
{
    private NativeArray<KdTreeNode> kDNodes;
    private KdTreeSimple kdTreeSimple;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        kdTreeSimple = new KdTreeSimple();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        BoidCellMapUpdate boidCellMapUpdate = SystemAPI.GetSingleton<BoidCellMapUpdate>();
        NativeHashMap<int3, CellDataMap> mapCellDatas = boidCellMapUpdate.mapCellDatas;
        foreach(KVPair<int3, CellDataMap> kVPair in mapCellDatas)
        {
            CellDataMap cellDataMap = kVPair.Value;
            if (!cellDataMap.hasKDTree || cellDataMap.kdTreeType != Enum.KDTreeType.Simple)
            {
                // If the KDTreeType is not Simple or has no KDTree, skip this cell
                continue;
            }
            else
            {
                kdTreeSimple.BuildTree(cellDataMap.boidPositions, Allocator.Temp);
                kDNodes = kdTreeSimple.GetNodes(Allocator.Temp);
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
