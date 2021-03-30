using Newtonsoft.Json;
using VRageMath;

namespace Utils.Torch
{
    public sealed class SerializableVector3
    {
        [JsonConstructor]
        SerializableVector3()
        {
        }

        public SerializableVector3(Vector3 v3d)
        {
            X = v3d.X;
            Y = v3d.Y;
            Z = v3d.Z;
        }

        public SerializableVector3(Vector3D v3d)
        {
            X = v3d.X;
            Y = v3d.Y;
            Z = v3d.Z;
        }

        [JsonProperty("x")]
        public double X { get; set; }

        [JsonProperty("y")]
        public double Y { get; set; }

        [JsonProperty("z")]
        public double Z { get; set; }

        public Vector3 ToVector3() => new Vector3(X, Y, Z);
        public Vector3D ToVector3D() => new Vector3D(X, Y, Z);
    }
}