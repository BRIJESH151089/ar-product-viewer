using System.Globalization;
using System.Text;

namespace ARProductViewer.Core
{
    /// <summary>
    /// Formats light estimation state as a short multi-line readout for on-device debugging:
    /// which mode is active, what the device reported, and what was applied to the scene light.
    /// </summary>
    public static class LightEstimateReadout
    {
        const string k_Missing = "–";

        /// <param name="mode">Active estimation mode as reported by the AR platform (e.g. "MainLightDirection, ...").</param>
        /// <param name="input">Latest values from the device.</param>
        /// <param name="applied">What the scene light is currently set to.</param>
        /// <param name="usingFallback">True when no estimate has arrived and fallback lighting is in use.</param>
        public static string Format(string mode, LightEstimateInput input, LightSettings applied, bool usingFallback)
        {
            var sb = new StringBuilder();
            sb.Append("Light estimation: ").Append(string.IsNullOrEmpty(mode) ? "None" : mode).AppendLine();
            sb.Append("Brightness: ").Append(Number(input.AverageBrightness))
              .Append("  Main light: ").Append(Number(input.MainLightBrightness)).AppendLine();
            sb.Append("Colour corr: ").Append(Rgb(input.ColorCorrection))
              .Append("  Temp: ").Append(input.ColorTemperatureKelvin.HasValue ? F(input.ColorTemperatureKelvin.Value, "0") + "K" : k_Missing).AppendLine();
            sb.Append("Direction: ").Append(input.MainLightDirection.HasValue ? Rgb(input.MainLightDirection) : k_Missing).AppendLine();
            sb.Append("Applied: ").Append(usingFallback ? "FALLBACK " : string.Empty)
              .Append("intensity ").Append(F(applied.Intensity, "0.00"))
              .Append("  colour ").Append(Rgb(applied.Color));
            return sb.ToString();
        }

        static string Number(float? value) => value.HasValue ? F(value.Value, "0.00") : k_Missing;

        static string Rgb(Float3? value) =>
            value.HasValue ? $"({F(value.Value.X, "0.00")}, {F(value.Value.Y, "0.00")}, {F(value.Value.Z, "0.00")})" : k_Missing;

        static string F(float value, string format) => value.ToString(format, CultureInfo.InvariantCulture);
    }
}
