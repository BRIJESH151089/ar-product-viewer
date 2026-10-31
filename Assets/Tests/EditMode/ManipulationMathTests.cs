using System;
using ARProductViewer.Core;
using NUnit.Framework;

namespace ARProductViewer.Tests
{
    public class ManipulationMathTests
    {
        const float k_Tolerance = 0.001f;

        // ---- Twist -----------------------------------------------------------------------------

        [Test]
        public void TwistDeltaDegrees_FingersTurnCounterClockwise_IsPositive()
        {
            // Line between fingers goes from pointing right (+X) to pointing up (+Y).
            var delta = ManipulationMath.TwistDeltaDegrees(
                new Float2(0f, 0f), new Float2(100f, 0f),
                new Float2(0f, 0f), new Float2(0f, 100f));

            Assert.AreEqual(90f, delta, k_Tolerance);
        }

        [Test]
        public void TwistDeltaDegrees_FingersTurnClockwise_IsNegative()
        {
            var delta = ManipulationMath.TwistDeltaDegrees(
                new Float2(0f, 0f), new Float2(100f, 0f),
                new Float2(0f, 0f), new Float2(0f, -100f));

            Assert.AreEqual(-90f, delta, k_Tolerance);
        }

        [Test]
        public void TwistDeltaDegrees_CrossingTheMinus180Seam_TakesShortWay()
        {
            // From 170 degrees to -170 degrees is a +20 degree turn, not -340.
            var delta = ManipulationMath.TwistDeltaDegrees(
                new Float2(0f, 0f), Polar(170f),
                new Float2(0f, 0f), Polar(-170f));

            Assert.AreEqual(20f, delta, k_Tolerance);
        }

        [Test]
        public void TwistDeltaDegrees_FingersTogether_IsZero()
        {
            var delta = ManipulationMath.TwistDeltaDegrees(
                new Float2(5f, 5f), new Float2(5.2f, 5f),
                new Float2(5f, 5f), new Float2(5f, 5.2f));

            Assert.AreEqual(0f, delta);
        }

        [TestCase(90f, 30f, 60f)]
        [TestCase(10f, 30f, 340f)]
        [TestCase(350f, -20f, 10f)]
        [TestCase(0f, 0f, 0f)]
        public void ApplyTwist_CounterClockwiseTwistLowersYaw_AndWraps(float yaw, float twist, float expected)
        {
            Assert.AreEqual(expected, ManipulationMath.ApplyTwist(yaw, twist), k_Tolerance);
        }

        static Float2 Polar(float degrees)
        {
            var radians = degrees * Math.PI / 180.0;
            return new Float2((float)(100.0 * Math.Cos(radians)), (float)(100.0 * Math.Sin(radians)));
        }

        // ---- Pinch -----------------------------------------------------------------------------

        [Test]
        public void PinchRatio_FingersSpreadToDoubleDistance_IsTwo()
        {
            var ratio = ManipulationMath.PinchRatio(
                new Float2(0f, 0f), new Float2(100f, 0f),
                new Float2(-50f, 0f), new Float2(150f, 0f));

            Assert.AreEqual(2f, ratio, k_Tolerance);
        }

        [Test]
        public void PinchRatio_DegenerateDistance_IsOne()
        {
            var ratio = ManipulationMath.PinchRatio(
                new Float2(0f, 0f), new Float2(0f, 0f),
                new Float2(0f, 0f), new Float2(100f, 0f));

            Assert.AreEqual(1f, ratio);
        }

        [Test]
        public void ApplyPinch_WithinRange_MultipliesScale()
        {
            Assert.AreEqual(1.5f, ManipulationMath.ApplyPinch(1f, 1.5f), k_Tolerance);
        }

        [Test]
        public void ApplyPinch_AboveMax_ClampsToMax()
        {
            Assert.AreEqual(ManipulationMath.MaxScale, ManipulationMath.ApplyPinch(1.8f, 1.5f), k_Tolerance);
        }

        [Test]
        public void ApplyPinch_BelowMin_ClampsToMin()
        {
            Assert.AreEqual(ManipulationMath.MinScale, ManipulationMath.ApplyPinch(0.6f, 0.5f), k_Tolerance);
        }

        [Test]
        public void ApplyPinch_DefaultRange_IsHalfToDouble()
        {
            Assert.AreEqual(0.5f, ManipulationMath.MinScale);
            Assert.AreEqual(2f, ManipulationMath.MaxScale);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        public void ApplyPinch_NonPositiveRatio_KeepsScale(float ratio)
        {
            Assert.AreEqual(1.2f, ManipulationMath.ApplyPinch(1.2f, ratio), k_Tolerance);
        }

        [Test]
        public void ApplyPinch_InvalidRange_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ManipulationMath.ApplyPinch(1f, 1f, 0f, 2f));
            Assert.Throws<ArgumentOutOfRangeException>(() => ManipulationMath.ApplyPinch(1f, 1f, 2f, 1f));
        }

        // ---- Drag threshold --------------------------------------------------------------------

        [TestCase(10f, false)]
        [TestCase(20f, false)]
        [TestCase(21f, true)]
        public void ExceedsDragThreshold_ComparesDistanceToThreshold(float moved, bool expected)
        {
            Assert.AreEqual(expected, ManipulationMath.ExceedsDragThreshold(new Float2(100f, 100f), new Float2(100f + moved, 100f), 20f));
        }

        // ---- Floor -----------------------------------------------------------------------------

        [Test]
        public void TryIntersectFloor_RayPointingDown_HitsFloorAtHeight()
        {
            var hit = ManipulationMath.TryIntersectFloor(new Float3(0f, 1.5f, 0f), new Float3(0f, -1f, 1f), -0.1f, out var point);

            Assert.IsTrue(hit);
            Assert.AreEqual(-0.1f, point.Y, k_Tolerance);
            Assert.AreEqual(1.6f, point.Z, k_Tolerance);
        }

        [Test]
        public void TryIntersectFloor_RayParallelOrPointingUp_Fails()
        {
            Assert.IsFalse(ManipulationMath.TryIntersectFloor(new Float3(0f, 1.5f, 0f), new Float3(0f, 0f, 1f), 0f, out _));
            Assert.IsFalse(ManipulationMath.TryIntersectFloor(new Float3(0f, 1.5f, 0f), new Float3(0f, 0.5f, 1f), 0f, out _));
        }

        [Test]
        public void KeepOnFloor_ReplacesOnlyHeight()
        {
            var kept = ManipulationMath.KeepOnFloor(new Float3(1f, 0.4f, 2f), -1.2f);

            Assert.AreEqual(new Float3(1f, -1.2f, 2f), kept);
            Assert.IsTrue(ManipulationMath.IsOnFloor(kept, -1.2f));
        }

        [Test]
        public void IsOnFloor_AboveFloor_IsFalse()
        {
            Assert.IsFalse(ManipulationMath.IsOnFloor(new Float3(0f, 0.05f, 0f), 0f));
        }
    }
}
