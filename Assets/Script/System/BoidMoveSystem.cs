using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
[UpdateAfter(typeof(BoidCaculaterDirectionSystem))]
partial struct BoidMoveSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((RefRW<LocalTransform> localTransform, RefRW<Boid> boid) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<Boid>>())
        {

            float3 direction = localTransform.ValueRO.Forward();
            float3 targetOffset = boid.ValueRO.targetPosition - localTransform.ValueRO.Position;
            float3 toTarget = float3.zero;

            float distanceToTarget = math.distancesq(boid.ValueRO.targetPosition, localTransform.ValueRO.Position);

            if (distanceToTarget > boid.ValueRO.orbitDistance)
            {
                // Nếu chưa gần mục tiêu thì hướng về mục tiêu
                toTarget = math.normalizesafe(targetOffset) * boid.ValueRO.targetAttractionWeight;
            }
            else
            {
                // Nếu đã gần thì quay vòng quanh mục tiêu
                boid.ValueRW.changeTargetTime -= SystemAPI.Time.DeltaTime;
                if (boid.ValueRW.changeTargetTime <= 0)
                {
                    // Thay đổi mục tiêu mới
                    Random random = boid.ValueRO.random;
                    boid.ValueRW.targetPosition = new float3(random.NextFloat(-5f, 5f), random.NextFloat(-5f, 5f), random.NextFloat(-5f, 5f));
                    boid.ValueRW.random = random; // Cập nhật lại random để tránh lặp lại cùng một mục tiêu
                    boid.ValueRW.changeTargetTime = boid.ValueRO.changeTargetTimeMax;
                }
                float3 tangent = float3.zero;
                if (math.lengthsq(targetOffset) > 0.0001f)
                {
                    float3 cross = math.cross(math.up(), targetOffset);
                    tangent = math.normalizesafe(cross);
                }
            }
            direction += toTarget;
            direction += boid.ValueRO.direction;
            if (math.lengthsq(direction) > 0.0001f)
            {
                quaternion currentRotation = localTransform.ValueRO.Rotation;
                quaternion targetRotation = quaternion.LookRotation(direction, math.up());
                localTransform.ValueRW.Rotation = math.slerp(currentRotation, targetRotation, SystemAPI.Time.DeltaTime * boid.ValueRO.rotationSpeed);
            }
            localTransform.ValueRW.Position += direction * boid.ValueRO.speed * SystemAPI.Time.DeltaTime;
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
