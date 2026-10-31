using System;

namespace ARProductViewer.Core
{
    /// <summary>
    /// Keeps a placed product at the pose the user chose, and only then lets it follow its AR anchor's
    /// drift corrections. Anchor reports are ignored while the anchor is not tracking, until it has been
    /// seen close to the intended pose, and when they jump implausibly far in one update. This prevents
    /// a not-yet-tracking anchor (often reported at the session origin) from dragging the product away.
    /// The product always stays upright: only position and yaw are followed.
    /// </summary>
    public sealed class AnchorFollower
    {
        public const float DefaultLockDistance = 0.25f;
        public const float DefaultMaxJump = 0.5f;

        readonly float m_LockDistance;
        readonly float m_MaxJump;

        Float3 m_LocalOffset;
        float m_LocalYaw;

        /// <summary>The pose the user placed the product at.</summary>
        public PlacementPose IntendedPose { get; }

        /// <summary>Where the product should be right now.</summary>
        public PlacementPose Pose { get; private set; }

        /// <summary>True once the anchor has been seen tracking close to the intended pose.</summary>
        public bool IsLocked { get; private set; }

        public AnchorFollower(PlacementPose intendedPose, float lockDistance = DefaultLockDistance, float maxJump = DefaultMaxJump)
        {
            if (lockDistance < 0f)
                throw new ArgumentOutOfRangeException(nameof(lockDistance));
            if (maxJump <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maxJump));

            IntendedPose = intendedPose;
            Pose = intendedPose;
            m_LockDistance = lockDistance;
            m_MaxJump = maxJump;
        }

        /// <summary>
        /// Feeds the anchor's latest state and returns the pose the product should use.
        /// </summary>
        public PlacementPose Update(bool anchorIsTracking, PlacementPose anchorPose)
        {
            if (!anchorIsTracking)
                return Pose;

            if (!IsLocked)
            {
                if (Float3.Distance(anchorPose.Position, IntendedPose.Position) > m_LockDistance)
                    return Pose;

                // Express the intended pose in the anchor's frame so later anchor corrections carry it along.
                m_LocalOffset = RotateYaw(IntendedPose.Position - anchorPose.Position, -anchorPose.YawDegrees);
                m_LocalYaw = IntendedPose.YawDegrees - anchorPose.YawDegrees;
                IsLocked = true;
                return Pose;
            }

            var candidate = new PlacementPose(
                anchorPose.Position + RotateYaw(m_LocalOffset, anchorPose.YawDegrees),
                NormalizeYaw(anchorPose.YawDegrees + m_LocalYaw));

            if (Float3.Distance(candidate.Position, Pose.Position) > m_MaxJump)
                return Pose;

            Pose = candidate;
            return Pose;
        }

        /// <summary>
        /// Rotates <paramref name="v"/> around the world up axis by <paramref name="yawDegrees"/>,
        /// using the same convention as a Unity Y rotation (+Z turns towards +X for positive yaw).
        /// </summary>
        public static Float3 RotateYaw(Float3 v, float yawDegrees)
        {
            var radians = yawDegrees * (Math.PI / 180.0);
            var cos = Math.Cos(radians);
            var sin = Math.Sin(radians);
            return new Float3(
                (float)(v.X * cos + v.Z * sin),
                v.Y,
                (float)(-v.X * sin + v.Z * cos));
        }

        static float NormalizeYaw(float yaw)
        {
            yaw %= 360f;
            return yaw < 0f ? yaw + 360f : yaw;
        }
    }
}
