using System;

namespace ARProductViewer.Core
{
    /// <summary>
    /// Engine-agnostic 2D vector, used for screen-space gesture positions (pixels, y up).
    /// </summary>
    public readonly struct Float2 : IEquatable<Float2>
    {
        public readonly float X;
        public readonly float Y;

        public Float2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static Float2 operator -(Float2 a, Float2 b) => new Float2(a.X - b.X, a.Y - b.Y);

        public float Length => (float)Math.Sqrt(X * X + Y * Y);

        public static float Distance(Float2 a, Float2 b) => (a - b).Length;

        public bool Equals(Float2 other) => X == other.X && Y == other.Y;

        public override bool Equals(object obj) => obj is Float2 other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public override string ToString() => $"({X}, {Y})";
    }
}
