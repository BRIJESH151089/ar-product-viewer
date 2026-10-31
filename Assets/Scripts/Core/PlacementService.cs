using System;
using System.Collections.Generic;

namespace ARProductViewer.Core
{
    /// <summary>
    /// Tracks the current placement candidate and decides whether a product can be placed.
    /// Only surfaces at floor height are accepted, so products never end up on beds or table tops.
    /// Feed it the surface hits along the aiming ray every frame; confirm with <see cref="TryConfirm"/>.
    /// </summary>
    public sealed class PlacementService
    {
        /// <summary>Default maximum height difference, in metres, between a hit and the floor.</summary>
        public const float DefaultFloorTolerance = 0.15f;

        const float k_MinFacingDistance = 0.001f;

        /// <summary>Maximum height difference, in metres, between a hit and the floor for it to count as floor.</summary>
        public float FloorTolerance { get; }

        /// <summary>True when the latest update found a floor-level surface a product can be placed on.</summary>
        public bool CanPlace { get; private set; }

        /// <summary>The placement pose for the latest valid hit. Only meaningful while <see cref="CanPlace"/> is true.</summary>
        public PlacementPose CurrentPose { get; private set; }

        /// <summary>Index of the hit chosen by the latest <see cref="UpdateHits"/> call, or -1 when none was valid.</summary>
        public int SelectedHitIndex { get; private set; } = -1;

        public PlacementService(float floorTolerance = DefaultFloorTolerance)
        {
            if (floorTolerance < 0f)
                throw new ArgumentOutOfRangeException(nameof(floorTolerance), "Floor tolerance must not be negative.");

            FloorTolerance = floorTolerance;
        }

        /// <summary>
        /// Picks the first hit (hits are expected closest first) that lies on a horizontal surface at floor
        /// height, skipping higher surfaces such as table tops. The resulting pose is upright and turned to
        /// face <paramref name="viewerPosition"/> (usually the camera).
        /// </summary>
        public void UpdateHits(IReadOnlyList<SurfaceHit> hits, Float3 viewerPosition, FloorEstimate floor)
        {
            if (hits == null)
                throw new ArgumentNullException(nameof(hits));

            for (var i = 0; i < hits.Count; i++)
            {
                if (IsAtFloorLevel(hits[i], floor))
                {
                    Accept(i, hits[i], viewerPosition);
                    return;
                }
            }

            Reject();
        }

        /// <summary>Single-hit variant of <see cref="UpdateHits"/>.</summary>
        public void UpdateHit(SurfaceHit hit, Float3 viewerPosition, FloorEstimate floor)
        {
            if (IsAtFloorLevel(hit, floor))
                Accept(0, hit, viewerPosition);
            else
                Reject();
        }

        /// <summary>
        /// True when <paramref name="hit"/> is on an upward-facing horizontal surface within
        /// <see cref="FloorTolerance"/> of the floor height.
        /// </summary>
        public bool IsAtFloorLevel(SurfaceHit hit, FloorEstimate floor)
        {
            return hit.HasHit
                && hit.IsHorizontalUp
                && floor.HasFloor
                && Math.Abs(hit.Position.Y - floor.Height) <= FloorTolerance;
        }

        /// <summary>
        /// Returns the pose to place a product at, or false when there is no valid floor surface.
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

        void Accept(int index, SurfaceHit hit, Float3 viewerPosition)
        {
            var yaw = ComputeYawTowards(hit.Position, viewerPosition, CurrentPose.YawDegrees);
            CurrentPose = new PlacementPose(hit.Position, yaw);
            SelectedHitIndex = index;
            CanPlace = true;
        }

        void Reject()
        {
            SelectedHitIndex = -1;
            CanPlace = false;
        }
    }
}
