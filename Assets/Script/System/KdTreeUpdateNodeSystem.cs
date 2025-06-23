using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static BoidCellMapUpdateSystem;
[UpdateAfter(typeof(BoidCellMapUpdateSystem))]
partial struct KdTreeUpdateNodeSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach((RefRO<BoidCellMapUpdate> boidCellMapUpdate, RefRW<KdTreeSimpleTag> kdTreeSimpleTag) in SystemAPI.Query<RefRO<BoidCellMapUpdate>, RefRW<KdTreeSimpleTag>>())
        {
            kdTreeSimpleTag.ValueRW.length = boidCellMapUpdate.ValueRO.mapCellDatas.Count;
        }
        foreach((RefRO<BoidCellMapUpdate> boidCellMapUpdate, RefRW<KdTreeIncrementalTag> kdTreeIncrementalTag) in SystemAPI.Query<RefRO<BoidCellMapUpdate>, RefRW<KdTreeIncrementalTag>>())
        {
            kdTreeIncrementalTag.ValueRW.length = boidCellMapUpdate.ValueRO.mapCellDatas.Count;
        }
        foreach((RefRO<BoidCellMapUpdate> boidCellMapUpdate, RefRW<KdTreeBalancedTag> kdTreeBalancedTag) in SystemAPI.Query<RefRO<BoidCellMapUpdate>, RefRW<KdTreeBalancedTag>>())
        {
            kdTreeBalancedTag.ValueRW.length = boidCellMapUpdate.ValueRO.mapCellDatas.Count;
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
