using ARProductViewer.Core;
using NUnit.Framework;

namespace ARProductViewer.Tests
{
    public class LightEstimateReadoutTests
    {
        static readonly LightSettings k_Applied =
            new LightSettings(1.04f, new Float3(1f, 0.95f, 0.88f), false, new Float3(0f, -1f, 0f), new Float3(0.4f, 0.4f, 0.4f));

        [Test]
        public void Format_AmbientModeValues_ShowsBrightnessAndColour()
        {
            var text = LightEstimateReadout.Format(
                "AmbientIntensity, AmbientColor",
                new LightEstimateInput { AverageBrightness = 0.52f, ColorCorrection = new Float3(0.9f, 1f, 0.8f) },
                k_Applied,
                false);

            StringAssert.Contains("AmbientIntensity, AmbientColor", text);
            StringAssert.Contains("Brightness: 0.52", text);
            StringAssert.Contains("Colour corr: (0.90, 1.00, 0.80)", text);
            StringAssert.Contains("Applied: intensity 1.04", text);
            StringAssert.DoesNotContain("FALLBACK", text);
        }

        [Test]
        public void Format_NothingReported_ShowsDashesNoneAndFallback()
        {
            var text = LightEstimateReadout.Format(null, new LightEstimateInput(), LightEstimationMapper.Fallback, true);

            StringAssert.Contains("Light estimation: None", text);
            StringAssert.Contains("Brightness: –", text);
            StringAssert.Contains("Direction: –", text);
            StringAssert.Contains("Applied: FALLBACK", text);
        }

        [Test]
        public void Format_HdrDirection_IsShown()
        {
            var text = LightEstimateReadout.Format("MainLightDirection", new LightEstimateInput { MainLightDirection = new Float3(0f, -0.6f, 0.8f) }, k_Applied, false);

            StringAssert.Contains("Direction: (0.00, -0.60, 0.80)", text);
        }

        [Test]
        public void Format_UsesInvariantDecimalPoint()
        {
            var previous = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
                var text = LightEstimateReadout.Format("x", new LightEstimateInput { AverageBrightness = 0.5f }, k_Applied, false);
                StringAssert.Contains("Brightness: 0.50", text);
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = previous;
            }
        }
    }
}
