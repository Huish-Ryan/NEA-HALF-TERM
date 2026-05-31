#nullable enable

namespace NEA.Models
{
    public class CoordRow
    {
        public string BodyName { get; set; } = "";
        public float X { get; set; }
        public float Y { get; set; }
        public float D1 { get; set; }
        public float D2 { get; set; }
        public float VelMagnitude { get; set; }
        public string Direction { get; set; } = "";
    }
}
