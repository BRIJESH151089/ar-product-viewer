namespace ARProductViewer.Core
{
    /// <summary>
    /// Best guess of the real-world floor height.
    /// </summary>
    public readonly struct FloorEstimate
    {
        public readonly bool HasFloor;

        /// <summary>World-space floor height in metres. Only meaningful when <see cref="HasFloor"/> is true.</summary>
        public readonly float Height;

        /// <summary>True when the height comes from a plane the platform classified as floor.</summary>
        public readonly bool IsFromClassification;

        public FloorEstimate(bool hasFloor, float height, bool isFromClassification)
        {
            HasFloor = hasFloor;
            Height = height;
            IsFromClassification = isFromClassification;
        }

        public static FloorEstimate None => new FloorEstimate(false, 0f, false);

        public static FloorEstimate At(float height) => new FloorEstimate(true, height, false);
    }
}
