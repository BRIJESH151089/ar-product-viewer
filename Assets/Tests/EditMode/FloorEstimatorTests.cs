using System;
using ARProductViewer.Core;
using NUnit.Framework;

namespace ARProductViewer.Tests
{
    public class FloorEstimatorTests
    {
        const float k_Tolerance = 0.0001f;

        [Test]
        public void Estimate_NoPlanes_HasNoFloor()
        {
            var floor = FloorEstimator.Estimate(new HorizontalPlane[0]);

            Assert.IsFalse(floor.HasFloor);
        }

        [Test]
        public void Estimate_UnclassifiedPlanes_UsesLowestPlane()
        {
            var floor = FloorEstimator.Estimate(new[]
            {
                new HorizontalPlane(-0.85f, false), // table top
                new HorizontalPlane(-1.45f, false), // floor
                new HorizontalPlane(-0.95f, false), // bed
            });

            Assert.IsTrue(floor.HasFloor);
            Assert.IsFalse(floor.IsFromClassification);
            Assert.AreEqual(-1.45f, floor.Height, k_Tolerance);
        }

        [Test]
        public void Estimate_ClassifiedFloorPresent_PrefersItOverLowerUnclassifiedPlane()
        {
            // A lower unclassified plane (e.g. a sunken step or noise) must not win over a classified floor.
            var floor = FloorEstimator.Estimate(new[]
            {
                new HorizontalPlane(-1.40f, true),
                new HorizontalPlane(-1.70f, false),
                new HorizontalPlane(-0.80f, false),
            });

            Assert.IsTrue(floor.IsFromClassification);
            Assert.AreEqual(-1.40f, floor.Height, k_Tolerance);
        }

        [Test]
        public void Estimate_SeveralClassifiedFloors_UsesLowestClassified()
        {
            var floor = FloorEstimator.Estimate(new[]
            {
                new HorizontalPlane(-1.38f, true),
                new HorizontalPlane(-1.42f, true),
            });

            Assert.AreEqual(-1.42f, floor.Height, k_Tolerance);
        }

        [Test]
        public void Estimate_SinglePlane_UsesIt()
        {
            var floor = FloorEstimator.Estimate(new[] { new HorizontalPlane(0.3f, false) });

            Assert.IsTrue(floor.HasFloor);
            Assert.AreEqual(0.3f, floor.Height, k_Tolerance);
        }

        [Test]
        public void Estimate_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => FloorEstimator.Estimate(null));
        }
    }
}
