using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
public struct BoidHashing : IComponentData
{
    public NativeParallelMultiHashMap<int3, Entity> mapBoid;
    public float cellSize;
}
partial struct BoidHashingSystem : ISystem
{
    private Entity boidHashingEntity;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        boidHashingEntity = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponentData(boidHashingEntity, new BoidHashing
        {
            mapBoid = new NativeParallelMultiHashMap<int3, Entity>(10000, Allocator.Persistent),
            cellSize = 5f // Set the cell size as needed
        });
        state.EntityManager.SetName(boidHashingEntity, "BoidHashing");
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        RefRW<BoidHashing> boidHashing = SystemAPI.GetComponentRW<BoidHashing>(boidHashingEntity);
        boidHashing.ValueRW.mapBoid.Clear();
        foreach ((RefRO<LocalTransform> localTransform, RefRO<Boid> boid, Entity boidEnity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<Boid>>().WithEntityAccess())
        {
            int3 cellPosition = (int3)math.floor(localTransform.ValueRO.Position / boidHashing.ValueRW.cellSize);
            boidHashing.ValueRW.mapBoid.Add(cellPosition, boidEnity);
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        if (SystemAPI.HasSingleton<BoidHashing>())
        {
            BoidHashing boidHashing = SystemAPI.GetSingleton<BoidHashing>();
            boidHashing.mapBoid.Dispose();
            state.EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<BoidHashing>());
        }
    }
}
