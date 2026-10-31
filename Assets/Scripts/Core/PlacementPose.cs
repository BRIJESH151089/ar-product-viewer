namespace ARProductViewer.Core
{
    /// <summary>
    /// Upright placement pose: a world position plus a rotation around the world up axis only,
    /// so a product always sits flat on the floor.
    /// </summary>
    public readonly struct PlacementPose
    {
        public readonly Float3 Position;

        /// <summary>Rotation around world up, in degrees [0, 360). 0 means the product faces +Z.</summary>
        public readonly float YawDegrees;

        public PlacementPose(Float3 position, float yawDegrees)
        {
            Position = position;
            YawDegrees = yawDegrees;
        }
    }
}
