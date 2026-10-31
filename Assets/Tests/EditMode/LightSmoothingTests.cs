using ARProductViewer.Core;
using NUnit.Framework;

namespace ARProductViewer.Tests
{
    public class LightSmoothingTests
    {
        const float k_Tolerance = 0.0001f;

        [Test]
        public void Factor_OneHalfLife_MovesHalfway()
        {
            Assert.AreEqual(0.5f, LightSmoothing.Factor(0.3f, 0.3f), k_Tolerance);
        }

        [Test]
        public void Factor_IsFrameRateIndependent()
        {
            // Two 30fps steps must land where one 15fps step does.
            var f30 = LightSmoothing.Factor(1f / 30f, 0.25f);
            var twoSteps = LightSmoothing.Lerp(LightSmoothing.Lerp(0f, 1f, f30), 1f, f30);
            var oneStep = LightSmoothing.Lerp(0f, 1f, LightSmoothing.Factor(2f / 30f, 0.25f));

            Assert.AreEqual(oneStep, twoSteps, k_Tolerance);
        }

        [TestCase(0f, 0.25f, 0f)]
        [TestCase(0.1f, 0f, 1f)]
        [TestCase(0.1f, -1f, 1f)]
        public void Factor_EdgeCases(float deltaTime, float halfLife, float expected)
        {
            Assert.AreEqual(expected, LightSmoothing.Factor(deltaTime, halfLife), k_Tolerance);
        }

        [Test]
        public void Smooth_MovesEveryFieldTowardsTarget()
        {
            var current = new LightSettings(1f, new Float3(1f, 1f, 1f), true, new Float3(0f, -1f, 0f), new Float3(0.4f, 0.4f, 0.4f));
            var target = new LightSettings(2f, new Float3(1f, 0.5f, 0.5f), true, new Float3(0f, 0f, 1f), new Float3(0.8f, 0.4f, 0.4f));

            var smoothed = LightSmoothing.Smooth(current, target, 0.5f, 0.5f);

            Assert.AreEqual(1.5f, smoothed.Intensity, k_Tolerance);
            Assert.AreEqual(0.75f, smoothed.Color.Y, k_Tolerance);
            Assert.AreEqual(0.5f, smoothed.Direction.Z, k_Tolerance);
            Assert.AreEqual(0.6f, smoothed.AmbientColor.X, k_Tolerance);
        }

        [Test]
        public void Smooth_TargetWithoutDirection_KeepsCurrentDirection()
        {
            var current = new LightSettings(1f, new Float3(1f, 1f, 1f), true, new Float3(0.6f, -0.8f, 0f), new Float3(0.4f, 0.4f, 0.4f));
            var target = new LightSettings(1f, new Float3(1f, 1f, 1f), false, new Float3(0f, -1f, 0f), new Float3(0.4f, 0.4f, 0.4f));

            var smoothed = LightSmoothing.Smooth(current, target, 1f, 0.1f);

            Assert.IsTrue(smoothed.HasDirection);
            Assert.AreEqual(0.6f, smoothed.Direction.X, k_Tolerance);
        }
    }
}
