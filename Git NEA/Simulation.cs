#nullable enable
using NEA.Models;
using System;
using System.Numerics;

namespace NEA.Simulation
{
    public class NBodySimulation
    {
        public Body[] Bodies { get; }
        public float dt { get; set; }
        public float Gravity { get; set; } = 1.0f;

        // Collisions now OFF by default
        public bool CollisionsEnabled { get; set; } = false;

        public NBodySimulation(Body[] bodies, float initialDt)
        {
            Bodies = bodies;
            dt = initialDt;
        }

        public void Step()
        {
            int n = Bodies.Length;
            Vector3[] accelerations = new Vector3[n];

            // ------------------------------------------------------------
            // 1. Compute gravitational acceleration for each body (true n-body)
            // ------------------------------------------------------------
            for (int i = 0; i < n; i++)
            {
                Vector3 acc = Vector3.Zero;

                for (int j = 0; j < n; j++)
                {
                    if (i == j) continue;

                    Vector3 dir = Bodies[j].position - Bodies[i].position;
                    float distSq = dir.LengthSquared();
                    if (distSq < 1e-6f) continue;

                    float invDist = 1.0f / MathF.Sqrt(distSq);
                    Vector3 dirNorm = dir * invDist;

                    float force = Gravity * Bodies[j].mass / distSq;
                    acc += dirNorm * force;
                }

                accelerations[i] = acc;
            }

            // ------------------------------------------------------------
            // 2. Update velocities
            // ------------------------------------------------------------
            for (int i = 0; i < n; i++)
                Bodies[i].velocity += accelerations[i] * dt;

            // ------------------------------------------------------------
            // 3. Update positions
            // ------------------------------------------------------------
            for (int i = 0; i < n; i++)
                Bodies[i].position += Bodies[i].velocity * dt;

            // ------------------------------------------------------------
            // 4. Optional collisions (only if checkbox is ticked)
            // ------------------------------------------------------------
            if (CollisionsEnabled)
                HandleCollisions();
        }

        private void HandleCollisions()
        {
            const float minDistance = 5f;

            for (int i = 0; i < Bodies.Length; i++)
            {
                for (int j = i + 1; j < Bodies.Length; j++)
                {
                    float dist = Vector3.Distance(Bodies[i].position, Bodies[j].position);

                    if (dist < minDistance)
                    {
                        // Simple elastic swap
                        (Bodies[i].velocity, Bodies[j].velocity) =
                            (Bodies[j].velocity, Bodies[i].velocity);
                    }
                }
            }
        }
    }
}
