using System;

namespace ARProductViewer.Core
{
    /// <summary>
    /// Pure functions for manipulating a placed product: two-finger twist and pinch, drag thresholds,
    /// and keeping moved positions on the floor.
    /// </summary>
    public static class ManipulationMath
    {
        /// <summary>Smallest allowed uniform scale, relative to the product's real size.</summary>
        public const float MinScale = 0.5f;

        /// <summary>Largest allowed uniform scale, relative to the product's real size.</summary>
        public const float MaxScale = 2f;

        const float k_MinFingerDistance = 1f;
        const float k_FloorEpsilon = 0.0001f;

        /// <summary>
        /// Signed change, in degrees within [-180, 180], of the angle of the line between two fingers.
        /// Positive means the fingers turned counter-clockwise on screen (screen y up).
        /// </summary>
        public static float TwistDeltaDegrees(Float2 previousA, Float2 previousB, Float2 currentA, Float2 currentB)
        {
            var previous = previousB - previousA;
            var current = currentB - currentA;
            if (previous.Length < k_MinFingerDistance || current.Length < k_MinFingerDistance)
                return 0f;

            var delta = (Math.Atan2(current.Y, current.X) - Math.Atan2(previous.Y, previous.X)) * (180.0 / Math.PI);
            return (float)WrapDegrees(delta);
        }

        /// <summary>
        /// Applies a screen-space twist to a yaw. A counter-clockwise twist turns the product
        /// counter-clockwise as seen from above, which is a negative yaw change. Returns [0, 360).
        /// </summary>
        public static float ApplyTwist(float yawDegrees, float counterClockwiseTwistDegrees)
        {
            var yaw = (yawDegrees - counterClockwiseTwistDegrees) % 360f;
            return yaw < 0f ? yaw + 360f : yaw;
        }

        /// <summary>
        /// Ratio of the current to the previous distance between two fingers (above 1 means spreading).
        /// Returns 1 when either distance is too small to be meaningful.
        /// </summary>
        public static float PinchRatio(Float2 previousA, Float2 previousB, Float2 currentA, Float2 currentB)
        {
            var previous = Float2.Distance(previousA, previousB);
            var current = Float2.Distance(currentA, currentB);
            if (previous < k_MinFingerDistance || current < k_MinFingerDistance)
                return 1f;

            return current / previous;
        }

        /// <summary>
        /// Multiplies <paramref name="scale"/> by <paramref name="pinchRatio"/> and clamps the result to
        /// [<paramref name="min"/>, <paramref name="max"/>]. Non-positive ratios leave the scale unchanged.
        /// </summary>
        public static float ApplyPinch(float scale, float pinchRatio, float min = MinScale, float max = MaxScale)
        {
            if (min <= 0f || max < min)
                throw new ArgumentOutOfRangeException(nameof(min), "Scale range must be positive and ordered.");

            var next = pinchRatio > 0f ? scale * pinchRatio : scale;
            return Math.Max(min, Math.Min(max, next));
        }

        /// <summary>True once a finger has moved further than <paramref name="thresholdPixels"/> from where it started.</summary>
        public static bool ExceedsDragThreshold(Float2 start, Float2 current, float thresholdPixels)
        {
            return Float2.Distance(start, current) > thresholdPixels;
        }

        /// <summary>
        /// Intersects a ray with the horizontal plane at <paramref name="floorHeight"/>. Fails when the ray is
        /// parallel to the floor or the floor is behind the ray origin.
        /// </summary>
        public static bool TryIntersectFloor(Float3 origin, Float3 direction, float floorHeight, out Float3 point)
        {
            point = default;
            if (Math.Abs(direction.Y) < k_FloorEpsilon)
                return false;

            var t = (floorHeight - origin.Y) / direction.Y;
            if (t <= 0f)
                return false;

            point = new Float3(origin.X + direction.X * t, floorHeight, origin.Z + direction.Z * t);
            return true;
        }

        /// <summary>Returns <paramref name="position"/> moved vertically onto the floor.</summary>
        public static Float3 KeepOnFloor(Float3 position, float floorHeight) => new Float3(position.X, floorHeight, position.Z);

        /// <summary>True when <paramref name="position"/> is at <paramref name="floorHeight"/> within <paramref name="tolerance"/>.</summary>
        public static bool IsOnFloor(Float3 position, float floorHeight, float tolerance = 0.001f)
        {
            return Math.Abs(position.Y - floorHeight) <= tolerance;
        }

        static double WrapDegrees(double degrees)
        {
            degrees %= 360.0;
            if (degrees > 180.0)
                degrees -= 360.0;
            else if (degrees < -180.0)
                degrees += 360.0;
            return degrees;
        }
    }
}
