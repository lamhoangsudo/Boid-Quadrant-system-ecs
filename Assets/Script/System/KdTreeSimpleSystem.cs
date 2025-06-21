using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
[UpdateAfter(typeof(BoidCellMapUpdateSystem))]
partial struct KdTreeSimpleSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Ensure that BoidCellMapUpdateSystem has run before this system
        state.RequireForUpdate<BoidCellMapUpdateSystem.BoidCellMapUpdate>();
        BoidCellMapUpdateSystem.BoidCellMapUpdate boidCellMapUpdate = SystemAPI.GetSingleton<BoidCellMapUpdateSystem.BoidCellMapUpdate>();
        foreach (KVPair<int3, BoidCellMapUpdateSystem.CellDataMap> mapCellData in boidCellMapUpdate.mapCellDatas)
        {

        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
