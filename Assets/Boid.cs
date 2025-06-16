using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
namespace OOP.Boid
{
    public class Boid : MonoBehaviour
    {
        public float speed = 5f;
        public float neighborDistance = 3f;
        public float separationDistance = 1f;
        // Trọng số cho hành vi alignment
        public float alignmentWeight = 1f;
        // Trọng số cho hành vi cohesion
        public float cohesionWeight = 1f;
        // Trọng số cho hành vi separation
        public float separationWeight = 1.5f;

        private List<Boid> allBoids;
        public Transform target;
        public float targetAttractionWeight = 1f;
        public float orbitDistance = 5f;
        public float orbitStrength = 1f;
        public void SetBoids(List<Boid> boids, Transform target)
        {
            allBoids = boids;
            this.target = target;
        }

        void Update()
        {
            if (allBoids == null) return;

            Vector3 alignment = Vector3.zero;
            Vector3 cohesion = Vector3.zero;
            Vector3 separation = Vector3.zero;

            int neighborCount = 0;

            foreach (Boid other in allBoids)
            {
                // Bỏ qua chính nó
                if (other == this) continue;
                // Tính khoảng cách đến các Boid khác
                float dist = Vector3.Distance(transform.position, other.transform.position);
                if (dist < neighborDistance)
                {
                    alignment += other.transform.forward;
                    cohesion += other.transform.position;

                    if (dist < separationDistance)
                    {
                        separation += (transform.position - other.transform.position) / dist;
                    }

                    neighborCount++;
                }
            }
            // Nếu có Boid lân cận thì tính toán hướng
            if (neighborCount > 0)
            {
                alignment = alignment / neighborCount;
                alignment = alignment.normalized * alignmentWeight;

                cohesion = (cohesion / neighborCount - transform.position).normalized * cohesionWeight;
                separation = separation.normalized * separationWeight;
            }
            // Nếu không có Boid lân cận thì giữ nguyên hướng
            else
            {
                alignment = transform.forward * alignmentWeight;
                cohesion = Vector3.zero;
                separation = Vector3.zero;
            }
            // Tính toán hướng đến mục tiêu
            Vector3 targetOffset = target.position - transform.position;
            Vector3 toTarget = Vector3.zero;

            float distToTarget = targetOffset.magnitude;

            if (distToTarget > orbitDistance)
            {
                // Bay về gần mục tiêu nếu còn xa
                toTarget = targetOffset.normalized * targetAttractionWeight;
            }
            else
            {
                // Nếu đã gần thì quay vòng quanh mục tiêu
                Vector3 tangent = Vector3.Cross(Vector3.up, targetOffset).normalized;
                toTarget = tangent * orbitStrength;
            }
            // Tính toán hướng tổng hợp
            Vector3 direction = alignment + cohesion + separation;
            direction += toTarget;
            if (direction != Vector3.zero)
                transform.forward = Vector3.Lerp(transform.forward, direction, Time.deltaTime * 2f);

            transform.position += transform.forward * speed * Time.deltaTime;
        }
    }
}
