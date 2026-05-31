#nullable enable
using System.Collections.Generic;
using System.Numerics;

namespace NEA.Models
{
    public class Body
    {
        public Vector3 position;
        public Vector3 velocity;
        public float mass;
        public string hexColour = "#FFFFFF";

        public List<Vector3> Trail { get; } = new();

        public Body(Vector3 pos, Vector3 vel, float m, string colour)
        {
            position = pos;
            velocity = vel;
            mass = m;
            hexColour = colour;
        }
    }
}
