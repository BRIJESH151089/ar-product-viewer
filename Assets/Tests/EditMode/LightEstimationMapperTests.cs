using ARProductViewer.Core;
using NUnit.Framework;

namespace ARProductViewer.Tests
{
    public class LightEstimationMapperTests
    {
        const float k_Tolerance = 0.01f;

        static void AssertColor(Float3 expected, Float3 actual)
        {
            Assert.AreEqual(expected.X, actual.X, k_Tolerance, "R");
            Assert.AreEqual(expected.Y, actual.Y, k_Tolerance, "G");
            Assert.AreEqual(expected.Z, actual.Z, k_Tolerance, "B");
        }

        // ---- Intensity -------------------------------------------------------------------------

        [Test]
        public void Map_NoEstimate_UsesDefaultsAndKeepsDirection()
        {
            var settings = LightEstimationMapper.Map(new LightEstimateInput());

            Assert.AreEqual(LightEstimationMapper.DefaultIntensity, settings.Intensity, k_Tolerance);
            AssertColor(new Float3(1f, 1f, 1f), settings.Color);
            Assert.IsFalse(settings.HasDirection);
        }

        [Test]
        public void Map_NormallyLitRoom_GivesIntensityOne()
        {
            var settings = LightEstimationMapper.Map(new LightEstimateInput { AverageBrightness = 0.5f });

            Assert.AreEqual(1f, settings.Intensity, k_Tolerance);
        }

        [Test]
        public void Map_DarkRoom_ClampsToMinimumIntensity()
        {
            var settings = LightEstimationMapper.Map(new LightEstimateInput { AverageBrightness = 0.02f });

            Assert.AreEqual(LightEstimationMapper.MinIntensity, settings.Intensity, k_Tolerance);
        }

        [Test]
        public void Map_VeryBrightRoom_ClampsToMaximumIntensity()
        {
            var settings = LightEstimationMapper.Map(new LightEstimateInput { AverageBrightness = 3f });

            Assert.AreEqual(LightEstimationMapper.MaxIntensity, settings.Intensity, k_Tolerance);
        }

        [Test]
        public void Map_HdrMainLightBrightness_TakesPriorityOverAverageBrightness()
        {
            var settings = LightEstimationMapper.Map(new LightEstimateInput { AverageBrightness = 0.9f, MainLightBrightness = 0.3f });

            Assert.AreEqual(0.6f, settings.Intensity, k_Tolerance);
        }

        // ---- Colour ----------------------------------------------------------------------------

        [Test]
        public void Map_ColorCorrection_IsNormalisedToBrightestChannel()
        {
            var settings = LightEstimationMapper.Map(new LightEstimateInput { ColorCorrection = new Float3(0.9f, 1f, 0.6f) });

            AssertColor(new Float3(0.9f, 1f, 0.6f), settings.Color);
        }

        [Test]
        public void Map_HdrMainLightColor_KeepsHueButNotIntensity()
        {
            // Colour includes intensity in HDR mode; only the hue should end up in the light colour.
            var settings = LightEstimationMapper.Map(new LightEstimateInput { MainLightColor = new Float3(2.4f, 2f, 1.2f), ColorCorrection = new Float3(0f, 1f, 1f) });

            AssertColor(new Float3(1f, 2f / 2.4f, 0.5f), settings.Color);
        }

        [Test]
        public void Map_OnlyColorTemperature_UsesKelvinColour()
        {
            var settings = LightEstimationMapper.Map(new LightEstimateInput { ColorTemperatureKelvin = 2700f });

            Assert.AreEqual(1f, settings.Color.X, k_Tolerance);
            Assert.Less(settings.Color.Z, settings.Color.Y, "warm light has less blue than green");
        }

        [Test]
        public void Map_BlackColorCorrection_FallsBackToWhite()
        {
            var settings = LightEstimationMapper.Map(new LightEstimateInput { ColorCorrection = new Float3(0f, 0f, 0f) });

            AssertColor(new Float3(1f, 1f, 1f), settings.Color);
        }

        [Test]
        public void Map_Ambient_FollowsColourAndIntensity()
        {
            var settings = LightEstimationMapper.Map(new LightEstimateInput { AverageBrightness = 0.5f, ColorCorrection = new Float3(1f, 0.5f, 0.5f) });

            var share = LightEstimationMapper.AmbientShare;
            AssertColor(new Float3(share, 0.5f * share, 0.5f * share), settings.AmbientColor);
        }

        // ---- Direction -------------------------------------------------------------------------

        [Test]
        public void Map_MainLightDirection_IsNormalised()
        {
            var settings = LightEstimationMapper.Map(new LightEstimateInput { MainLightDirection = new Float3(0f, -3f, 4f) });

            Assert.IsTrue(settings.HasDirection);
            AssertColor(new Float3(0f, -0.6f, 0.8f), settings.Direction);
        }

        [Test]
        public void Map_ZeroDirection_IsIgnored()
        {
            var settings = LightEstimationMapper.Map(new LightEstimateInput { MainLightDirection = new Float3(0f, 0f, 0f) });

            Assert.IsFalse(settings.HasDirection);
        }

        // ---- Fallback --------------------------------------------------------------------------

        [Test]
        public void Fallback_IsNeutralIntensityWarmWhiteWithAmbient()
        {
            var fallback = LightEstimationMapper.Fallback;

            Assert.AreEqual(1f, fallback.Intensity, k_Tolerance);
            Assert.AreEqual(1f, fallback.Color.X, k_Tolerance, "red is the brightest channel of warm light");
            Assert.Greater(fallback.Color.Z, 0.75f, "warm-neutral, not orange");
            Assert.Less(fallback.Color.Z, fallback.Color.X);
            Assert.AreEqual(fallback.Color.Y * LightEstimationMapper.AmbientShare, fallback.AmbientColor.Y, k_Tolerance);
        }

        [Test]
        public void Fallback_KeepsAuthoredLightDirection()
        {
            Assert.IsFalse(LightEstimationMapper.Fallback.HasDirection);
        }

        // ---- Kelvin ----------------------------------------------------------------------------

        [Test]
        public void KelvinToRgb_Daylight_IsNearlyWhite()
        {
            var rgb = LightEstimationMapper.KelvinToRgb(6600f);

            Assert.Greater(rgb.X, 0.95f);
            Assert.Greater(rgb.Y, 0.95f);
            Assert.Greater(rgb.Z, 0.95f);
        }

        [Test]
        public void KelvinToRgb_Candlelight_IsOrange()
        {
            var rgb = LightEstimationMapper.KelvinToRgb(1900f);

            Assert.AreEqual(1f, rgb.X, k_Tolerance);
            Assert.Less(rgb.Z, 0.1f);
        }

        [Test]
        public void KelvinToRgb_ClearSky_IsBlueish()
        {
            var rgb = LightEstimationMapper.KelvinToRgb(12000f);

            Assert.AreEqual(1f, rgb.Z, k_Tolerance);
            Assert.Less(rgb.X, rgb.Z);
        }

        [Test]
        public void KelvinToRgb_OutOfRange_IsClamped()
        {
            AssertColor(LightEstimationMapper.KelvinToRgb(1000f), LightEstimationMapper.KelvinToRgb(10f));
            AssertColor(LightEstimationMapper.KelvinToRgb(40000f), LightEstimationMapper.KelvinToRgb(90000f));
        }
    }
}
