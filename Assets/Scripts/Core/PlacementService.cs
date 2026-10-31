using System;

namespace ARProductViewer.Core
{
    /// <summary>
    /// Tracks the current placement candidate and decides whether a product can be placed.
    /// Feed it a <see cref="SurfaceHit"/> every frame; confirm with <see cref="TryConfirm"/>.
    /// </summary>
    public sealed class PlacementService
    {
        const float k_MinFacingDistance = 0.001f;

        /// <summary>True when the latest hit is a surface a product can be placed on.</summary>
        public bool CanPlace { get; private set; }

        /// <summary>The placement pose for the latest valid hit. Only meaningful while <see cref="CanPlace"/> is true.</summary>
        public PlacementPose CurrentPose { get; private set; }

        /// <summary>
        /// Updates the placement candidate from a surface hit. The resulting pose is upright and
        /// turned to face <paramref name="viewerPosition"/> (usually the camera).
        /// </summary>
        public void UpdateHit(SurfaceHit hit, Float3 viewerPosition)
        {
            CanPlace = hit.HasHit && hit.IsHorizontalUp;
            if (!CanPlace)
                return;

            var yaw = ComputeYawTowards(hit.Position, viewerPosition, CurrentPose.YawDegrees);
            CurrentPose = new PlacementPose(hit.Position, yaw);
        }

        /// <summary>
        /// Returns the pose to place a product at, or false when there is no valid surface.
        /// </summary>
        public bool TryConfirm(out PlacementPose pose)
        {
            pose = CurrentPose;
            return CanPlace;
        }

        /// <summary>
        /// Yaw in degrees [0, 360) that turns +Z at <paramref name="from"/> towards <paramref name="target"/>
        /// on the horizontal plane. Returns <paramref name="fallbackYaw"/> when the target is directly above or below.
        /// </summary>
        public static float ComputeYawTowards(Float3 from, Float3 target, float fallbackYaw)
        {
            var delta = target - from;
            if (Math.Abs(delta.X) < k_MinFacingDistance && Math.Abs(delta.Z) < k_MinFacingDistance)
                return fallbackYaw;

            var yaw = (float)(Math.Atan2(delta.X, delta.Z) * (180.0 / Math.PI));
            return yaw < 0f ? yaw + 360f : yaw;
        }
    }
}
