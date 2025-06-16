using System.Collections.Generic;
using UnityEngine;
namespace OOP.BoidManager
{
    public class BoidManager : MonoBehaviour
    {
        public GameObject boidPrefab;
        public int boidCount = 50;
        public float spawnRadius = 10f;

        private List<OOP.Boid.Boid> boids = new List<OOP.Boid.Boid>();

        void Start()
        {
            for (int i = 0; i < boidCount; i++)
            {
                Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
                GameObject boidObj = Instantiate(boidPrefab, pos, Quaternion.identity);
                OOP.Boid.Boid boid = boidObj.GetComponent<OOP.Boid.Boid>();
                boids.Add(boid);
            }

            foreach (OOP.Boid.Boid b in boids)
            {
                b.SetBoids(boids, gameObject.transform);
            }
        }
    }
}
