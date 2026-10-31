namespace ARProductViewer.Core
{
    /// <summary>
    /// A detected upward-facing horizontal surface, reduced to what floor estimation needs.
    /// </summary>
    public readonly struct HorizontalPlane
    {
        /// <summary>World-space height of the surface, in metres.</summary>
        public readonly float Height;

        /// <summary>True when the AR platform has classified this surface as floor.</summary>
        public readonly bool IsClassifiedFloor;

        public HorizontalPlane(float height, bool isClassifiedFloor)
        {
            Height = height;
            IsClassifiedFloor = isClassifiedFloor;
        }
    }
}
