using System;
using System.Collections.Generic;

namespace ARProductViewer.Core
{
    /// <summary>
    /// Picks the floor height from the horizontal surfaces detected so far.
    /// </summary>
    public static class FloorEstimator
    {
        /// <summary>
        /// Uses the lowest plane classified as floor when the platform provides classifications;
        /// otherwise falls back to the lowest detected horizontal plane. Returns
        /// <see cref="FloorEstimate.None"/> when there are no planes.
        /// </summary>
        public static FloorEstimate Estimate(IEnumerable<HorizontalPlane> planes)
        {
            if (planes == null)
                throw new ArgumentNullException(nameof(planes));

            var hasAny = false;
            var lowestAny = float.MaxValue;
            var hasClassified = false;
            var lowestClassified = float.MaxValue;

            foreach (var plane in planes)
            {
                hasAny = true;
                lowestAny = Math.Min(lowestAny, plane.Height);

                if (plane.IsClassifiedFloor)
                {
                    hasClassified = true;
                    lowestClassified = Math.Min(lowestClassified, plane.Height);
                }
            }

            if (hasClassified)
                return new FloorEstimate(true, lowestClassified, true);

            return hasAny ? new FloorEstimate(true, lowestAny, false) : FloorEstimate.None;
        }
    }
}
