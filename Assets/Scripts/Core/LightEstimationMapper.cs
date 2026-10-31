using System;

namespace ARProductViewer.Core
{
    /// <summary>
    /// Maps AR light estimation values to settings for the scene's main light, so placed products are lit
    /// like the real room. HDR values (main light colour/brightness/direction) are preferred when present,
    /// then ambient brightness and colour correction, then colour temperature.
    /// </summary>
    public static class LightEstimationMapper
    {
        /// <summary>Intensity used when the estimate has no brightness at all.</summary>
        public const float DefaultIntensity = 1f;

        /// <summary>Intensity range the main light is kept within, so products never go black or blow out.</summary>
        public const float MinIntensity = 0.25f;
        public const float MaxIntensity = 2f;

        /// <summary>
        /// Ambient brightness of ~0.5 (a normally lit room) maps to intensity 1.
        /// </summary>
        public const float BrightnessToIntensity = 2f;

        /// <summary>Share of the main light's intensity used for flat ambient light.</summary>
        public const float AmbientShare = 0.45f;

        const float k_MinDirectionLength = 0.0001f;

        public static LightSettings Map(LightEstimateInput input)
        {
            var intensity = Clamp(EstimateIntensity(input), MinIntensity, MaxIntensity);
            var color = EstimateColor(input);

            var hasDirection = false;
            var direction = new Float3(0f, -1f, 0f);
            if (input.MainLightDirection.HasValue && TryNormalize(input.MainLightDirection.Value, out var normalized))
            {
                hasDirection = true;
                direction = normalized;
            }

            var ambientScale = intensity * AmbientShare;
            var ambient = new Float3(color.X * ambientScale, color.Y * ambientScale, color.Z * ambientScale);
            return new LightSettings(intensity, color, hasDirection, direction, ambient);
        }

        static float EstimateIntensity(LightEstimateInput input)
        {
            if (input.MainLightBrightness.HasValue)
                return input.MainLightBrightness.Value * BrightnessToIntensity;
            if (input.AverageBrightness.HasValue)
                return input.AverageBrightness.Value * BrightnessToIntensity;
            return DefaultIntensity;
        }

        static Float3 EstimateColor(LightEstimateInput input)
        {
            // Colour only: intensity is handled separately, so normalise to the brightest channel.
            if (input.MainLightColor.HasValue && TryNormalizeToMax(input.MainLightColor.Value, out var main))
                return main;
            if (input.ColorCorrection.HasValue && TryNormalizeToMax(input.ColorCorrection.Value, out var corrected))
                return corrected;
            if (input.ColorTemperatureKelvin.HasValue)
                return KelvinToRgb(input.ColorTemperatureKelvin.Value);
            return new Float3(1f, 1f, 1f);
        }

        /// <summary>
        /// Approximate RGB (each 0..1) of a black-body light at <paramref name="kelvin"/>, clamped to 1000-40000K.
        /// ~6600K is close to white; lower is warmer (orange), higher is cooler (blue).
        /// </summary>
        public static Float3 KelvinToRgb(float kelvin)
        {
            // Tanner Helland's curve fit of the black-body spectrum.
            var t = Clamp(kelvin, 1000f, 40000f) / 100.0;

            double r, g, b;
            if (t <= 66.0)
            {
                r = 255.0;
                g = 99.4708025861 * Math.Log(t) - 161.1195681661;
            }
            else
            {
                r = 329.698727446 * Math.Pow(t - 60.0, -0.1332047592);
                g = 288.1221695283 * Math.Pow(t - 60.0, -0.0755148492);
            }

            if (t >= 66.0)
                b = 255.0;
            else if (t <= 19.0)
                b = 0.0;
            else
                b = 138.5177312231 * Math.Log(t - 10.0) - 305.0447927307;

            return new Float3(Channel(r), Channel(g), Channel(b));
        }

        static float Channel(double value) => (float)(Math.Max(0.0, Math.Min(255.0, value)) / 255.0);

        static bool TryNormalizeToMax(Float3 color, out Float3 normalized)
        {
            var max = Math.Max(color.X, Math.Max(color.Y, color.Z));
            if (max <= 0f)
            {
                normalized = default;
                return false;
            }

            normalized = new Float3(Math.Max(0f, color.X / max), Math.Max(0f, color.Y / max), Math.Max(0f, color.Z / max));
            return true;
        }

        static bool TryNormalize(Float3 v, out Float3 normalized)
        {
            var length = (float)Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
            if (length < k_MinDirectionLength)
            {
                normalized = default;
                return false;
            }

            normalized = new Float3(v.X / length, v.Y / length, v.Z / length);
            return true;
        }

        static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
    }
}
