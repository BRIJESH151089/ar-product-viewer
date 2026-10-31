namespace ARProductViewer.Core
{
    /// <summary>
    /// Result of probing the real world for a surface, converted to plain data by the AR layer.
    /// </summary>
    public readonly struct SurfaceHit
    {
        public readonly bool HasHit;

        /// <summary>True when the hit surface is a horizontal, upward-facing plane (a floor or table top).</summary>
        public readonly bool IsHorizontalUp;

        public readonly Float3 Position;

        public SurfaceHit(bool hasHit, bool isHorizontalUp, Float3 position)
        {
            HasHit = hasHit;
            IsHorizontalUp = isHorizontalUp;
            Position = position;
        }

        public static SurfaceHit None => new SurfaceHit(false, false, Float3.Zero);

        public static SurfaceHit OnHorizontalPlane(Float3 position) => new SurfaceHit(true, true, position);
    }
}
