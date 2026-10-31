using System;
using System.Collections.Generic;

namespace ARProductViewer.Core
{
    /// <summary>
    /// Tracks the current placement candidate and decides whether a product can be placed.
    /// Only surfaces at floor height are accepted, so products never end up on beds or table tops,
    /// and the resulting pose is snapped onto the floor reference height so the preview and the
    /// placed product share one exact height.
    /// Feed it the surface hits along the aiming ray every frame; confirm with <see cref="TryConfirm"/>.
    /// </summary>
    public sealed class PlacementService
    {
        /// <summary>Default maximum height difference, in metres, between a hit and the floor.</summary>
        public const float DefaultFloorTolerance = 0.15f;

        const float k_MinFacingDistance = 0.001f;
        const float k_MinRayDrop = 0.001f;

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
        /// height, skipping higher surfaces such as table tops. The resulting pose sits exactly at the floor
        /// height on the aiming ray from <paramref name="viewerPosition"/> (the camera) through the hit, and is
        /// upright and turned to face the viewer.
        /// </summary>
        public void UpdateHits(IReadOnlyList<SurfaceHit> hits, Float3 viewerPosition, FloorEstimate floor)
        {
            if (hits == null)
                throw new ArgumentNullException(nameof(hits));

            for (var i = 0; i < hits.Count; i++)
            {
                if (IsAtFloorLevel(hits[i], floor))
                {
                    Accept(i, hits[i], viewerPosition, floor);
                    return;
                }
            }

            Reject();
        }

        /// <summary>Single-hit variant of <see cref="UpdateHits"/>.</summary>
        public void UpdateHit(SurfaceHit hit, Float3 viewerPosition, FloorEstimate floor)
        {
            if (IsAtFloorLevel(hit, floor))
                Accept(0, hit, viewerPosition, floor);
            else
                Reject();
        }

        /// <summary>
        /// Moves <paramref name="hit"/> along the ray from <paramref name="viewer"/> through it until it reaches
        /// <paramref name="floorHeight"/>, so the point stays under the same screen position but lies exactly
        /// on the floor. If the ray does not point downwards, only the height is replaced.
        /// </summary>
        public static Float3 SnapToFloor(Float3 hit, Float3 viewer, float floorHeight)
        {
            var drop = hit.Y - viewer.Y;
            if (drop > -k_MinRayDrop)
                return new Float3(hit.X, floorHeight, hit.Z);

            var t = (floorHeight - viewer.Y) / drop;
            return new Float3(
                viewer.X + (hit.X - viewer.X) * t,
                floorHeight,
                viewer.Z + (hit.Z - viewer.Z) * t);
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

        void Accept(int index, SurfaceHit hit, Float3 viewerPosition, FloorEstimate floor)
        {
            var position = SnapToFloor(hit.Position, viewerPosition, floor.Height);
            var yaw = ComputeYawTowards(position, viewerPosition, CurrentPose.YawDegrees);
            CurrentPose = new PlacementPose(position, yaw);
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
