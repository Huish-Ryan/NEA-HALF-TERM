#nullable enable

namespace NEA.Models
{
    public class PhysicalRow
    {
        public string BodyName { get; set; } = "";
        public float Mass { get; set; }
        public float VelX { get; set; }
        public float VelY { get; set; }
        public float VelZ { get; set; }
    }
}
