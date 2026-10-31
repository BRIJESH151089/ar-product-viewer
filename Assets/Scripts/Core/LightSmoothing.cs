using System;

namespace ARProductViewer.Core
{
    /// <summary>
    /// Frame-rate independent exponential smoothing, so light estimates change gradually instead of flickering.
    /// </summary>
    public static class LightSmoothing
    {
        /// <summary>
        /// Fraction (0..1) to move towards the target this frame so that half the remaining distance is
        /// covered every <paramref name="halfLifeSeconds"/>. A non-positive half-life snaps immediately.
        /// </summary>
        public static float Factor(float deltaTimeSeconds, float halfLifeSeconds)
        {
            if (halfLifeSeconds <= 0f)
                return 1f;
            if (deltaTimeSeconds <= 0f)
                return 0f;

            return (float)(1.0 - Math.Pow(0.5, deltaTimeSeconds / halfLifeSeconds));
        }

        public static float Lerp(float current, float target, float factor) => current + (target - current) * factor;

        public static Float3 Lerp(Float3 current, Float3 target, float factor) =>
            new Float3(Lerp(current.X, target.X, factor), Lerp(current.Y, target.Y, factor), Lerp(current.Z, target.Z, factor));

        /// <summary>Smooths every field of <paramref name="current"/> towards <paramref name="target"/>.</summary>
        public static LightSettings Smooth(LightSettings current, LightSettings target, float deltaTimeSeconds, float halfLifeSeconds)
        {
            var f = Factor(deltaTimeSeconds, halfLifeSeconds);
            var direction = target.HasDirection ? Lerp(current.Direction, target.Direction, f) : current.Direction;
            return new LightSettings(
                Lerp(current.Intensity, target.Intensity, f),
                Lerp(current.Color, target.Color, f),
                target.HasDirection || current.HasDirection,
                direction,
                Lerp(current.AmbientColor, target.AmbientColor, f));
        }
    }
}
