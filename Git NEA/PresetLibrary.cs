#nullable enable
using System.Numerics;
using NEA.Models;

namespace NEA.Simulation
{
    public static class PresetLibrary
    {
        // ============================================================
        // FIGURE EIGHT — classic 3-body stable orbit
        // ============================================================
        public static Preset FigureEight()
        {
            var bodies = new[]
            {
                new Body(new Vector3(-1.0f, 0.0f, 0.0f), new Vector3(0.347f, 0.532f, 0.0f), 1.0f, "#FF6666"),
                new Body(new Vector3(1.0f, 0.0f, 0.0f), new Vector3(0.347f, 0.532f, 0.0f), 1.0f, "#66CCFF"),
                new Body(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(-0.694f, -1.064f, 0.0f), 1.0f, "#FFFF66")
            };

            return new Preset(bodies, dt: 0.016f, gravity: 1.0f);
        }

        // ============================================================
        // CHAOTIC TRIPLE — unstable, chaotic motion
        // ============================================================
        public static Preset ChaoticTriple()
        {
            var bodies = new[]
            {
                new Body(new Vector3(-1.5f, 0.0f, 0.0f), new Vector3(0.0f, 0.8f, 0.0f), 1.0f, "#FFAA88"),
                new Body(new Vector3(1.5f, 0.0f, 0.0f), new Vector3(0.0f, -0.8f, 0.0f), 1.0f, "#88AAFF"),
                new Body(new Vector3(0.0f, 1.5f, 0.0f), new Vector3(-0.8f, 0.0f, 0.0f), 1.0f, "#AAFF88")
            };

            return new Preset(bodies, dt: 0.016f, gravity: 1.2f);
        }

        // ============================================================
        // NEAR COLLISION — two bodies nearly collide while third escapes
        // ============================================================
        public static Preset NearCollision()
        {
            var bodies = new[]
            {
                new Body(new Vector3(-0.5f, 0.0f, 0.0f), new Vector3(0.0f, 0.9f, 0.0f), 1.0f, "#FF8888"),
                new Body(new Vector3(0.5f, 0.0f, 0.0f), new Vector3(0.0f, -0.9f, 0.0f), 1.0f, "#8888FF"),
                new Body(new Vector3(0.0f, 1.2f, 0.0f), new Vector3(-0.6f, 0.0f, 0.0f), 0.8f, "#88FF88")
            };

            return new Preset(bodies, dt: 0.016f, gravity: 1.0f);
        }

        // ============================================================
        // STABLE ORBIT — one heavy body with two orbiting satellites
        // ============================================================
        public static Preset StableOrbit()
        {
            var bodies = new[]
            {
                new Body(new Vector3(0.0f, 0.0f, 0.0f), Vector3.Zero, 5.0f, "#FFD700"),
                new Body(new Vector3(3.0f, 0.0f, 0.0f), new Vector3(0.0f, 1.2f, 0.0f), 1.0f, "#66CCFF"),
                new Body(new Vector3(-4.5f, 0.0f, 0.0f), new Vector3(0.0f, -0.9f, 0.0f), 0.8f, "#FF6666")
            };

            return new Preset(bodies, dt: 0.016f, gravity: 1.0f);
        }

        // ============================================================
        // THREE EQUAL MASSES — symmetric triple orbit
        // ============================================================
        public static Preset ThreeEqualMasses()
        {
            var bodies = new[]
            {
                new Body(new Vector3(0.0f, 2.0f, 0.0f), new Vector3(-0.8f, 0.0f, 0.0f), 1.0f, "#FFAA77"),
                new Body(new Vector3(-1.732f, -1.0f, 0.0f), new Vector3(0.4f, 0.7f, 0.0f), 1.0f, "#77AAFF"),
                new Body(new Vector3(1.732f, -1.0f, 0.0f), new Vector3(0.4f, -0.7f, 0.0f), 1.0f, "#AAFF77")
            };

            return new Preset(bodies, dt: 0.016f, gravity: 1.0f);
        }

        // ============================================================
        // ALL PRESETS — helper for random selection
        // ============================================================
        public static Preset[] AllPresets() => new[]
        {
            FigureEight(),
            ChaoticTriple(),
            NearCollision(),
            StableOrbit(),
            ThreeEqualMasses()
        };
    }

    // ============================================================
    // PRESET STRUCTURE
    // ============================================================
    public class Preset
    {
        public Body[] Bodies { get; }
        public float Dt { get; }
        public float Gravity { get; }

        public Preset(Body[] bodies, float dt, float gravity)
        {
            Bodies = bodies;
            Dt = dt;
            Gravity = gravity;
        }
    }
}
