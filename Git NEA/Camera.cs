#nullable enable
using System.Numerics;

namespace NEA.Rendering
{
    public class Camera
    {
        public float Zoom { get; set; } = 1.0f;
        public float OffsetX { get; set; } = 0.0f;
        public float OffsetY { get; set; } = 0.0f;

        public bool Is3D { get; set; } = false;

        public (double x, double y) WorldToScreen(Vector3 p, double cx, double cy)
        {
            double sx = p.X * Zoom + OffsetX + cx;
            double sy = p.Y * Zoom + OffsetY + cy;
            return (sx, sy);
        }
    }
}
