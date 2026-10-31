using System;

namespace ARProductViewer.Core
{
    /// <summary>
    /// Engine-agnostic 3D vector. Units are metres in world space.
    /// </summary>
    public readonly struct Float3 : IEquatable<Float3>
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public Float3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Float3 Zero => new Float3(0f, 0f, 0f);

        public static Float3 operator -(Float3 a, Float3 b) => new Float3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public bool Equals(Float3 other) => X == other.X && Y == other.Y && Z == other.Z;

        public override bool Equals(object obj) => obj is Float3 other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y, Z);

        public override string ToString() => $"({X}, {Y}, {Z})";
    }
}
