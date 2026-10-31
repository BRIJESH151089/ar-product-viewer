using System;
using ARProductViewer.Core;
using NUnit.Framework;

namespace ARProductViewer.Tests
{
    public class PlacementServiceTests
    {
        const float k_Tolerance = 0.001f;
        const float k_FloorY = 0f;

        static readonly Float3 k_Camera = new Float3(0f, 1.5f, 0f);
        static readonly FloorEstimate k_Floor = FloorEstimate.At(k_FloorY);

        PlacementService m_Service;

        [SetUp]
        public void SetUp()
        {
            m_Service = new PlacementService();
        }

        static SurfaceHit FloorHit(float x, float z, float y = k_FloorY) => SurfaceHit.OnHorizontalPlane(new Float3(x, y, z));

        [Test]
        public void NewService_CannotPlace()
        {
            Assert.IsFalse(m_Service.CanPlace);
            Assert.IsFalse(m_Service.TryConfirm(out _));
            Assert.AreEqual(-1, m_Service.SelectedHitIndex);
        }

        [Test]
        public void UpdateHit_NoHit_CannotPlace()
        {
            m_Service.UpdateHit(SurfaceHit.None, k_Camera, k_Floor);

            Assert.IsFalse(m_Service.CanPlace);
            Assert.IsFalse(m_Service.TryConfirm(out _));
        }

        [Test]
        public void UpdateHit_HitOnNonHorizontalSurface_CannotPlace()
        {
            m_Service.UpdateHit(new SurfaceHit(true, false, new Float3(0f, k_FloorY, 2f)), k_Camera, k_Floor);

            Assert.IsFalse(m_Service.CanPlace);
        }

        [Test]
        public void UpdateHit_FloorHit_CanPlaceAtHitPosition()
        {
            var hit = FloorHit(0.3f, 2f);

            m_Service.UpdateHit(hit, k_Camera, k_Floor);

            Assert.IsTrue(m_Service.TryConfirm(out var pose));
            Assert.AreEqual(hit.Position, pose.Position);
        }

        [Test]
        public void UpdateHit_ValidThenNoHit_CannotPlace()
        {
            m_Service.UpdateHit(FloorHit(0f, 2f), k_Camera, k_Floor);
            m_Service.UpdateHit(SurfaceHit.None, k_Camera, k_Floor);

            Assert.IsFalse(m_Service.TryConfirm(out _));
        }

        [Test]
        public void UpdateHit_NoFloorKnown_CannotPlace()
        {
            m_Service.UpdateHit(FloorHit(0f, 2f), k_Camera, FloorEstimate.None);

            Assert.IsFalse(m_Service.CanPlace);
        }

        [TestCase(0.45f)] // coffee table
        [TestCase(0.55f)] // bed
        [TestCase(0.75f)] // dining table
        public void UpdateHit_ElevatedSurface_CannotPlace(float surfaceHeight)
        {
            m_Service.UpdateHit(FloorHit(0f, 2f, k_FloorY + surfaceHeight), k_Camera, k_Floor);

            Assert.IsFalse(m_Service.CanPlace);
        }

        [TestCase(0.14f)]
        [TestCase(-0.14f)]
        [TestCase(0.15f)]
        public void UpdateHit_WithinFloorTolerance_CanPlace(float heightOffset)
        {
            m_Service.UpdateHit(FloorHit(0f, 2f, k_FloorY + heightOffset), k_Camera, k_Floor);

            Assert.IsTrue(m_Service.CanPlace);
        }

        [TestCase(0.16f)]
        [TestCase(-0.16f)]
        public void UpdateHit_JustOutsideFloorTolerance_CannotPlace(float heightOffset)
        {
            m_Service.UpdateHit(FloorHit(0f, 2f, k_FloorY + heightOffset), k_Camera, k_Floor);

            Assert.IsFalse(m_Service.CanPlace);
        }

        [Test]
        public void UpdateHit_FloorAtNonZeroHeight_UsesFloorEstimate()
        {
            // AR session origin is usually near the device, so the floor sits well below zero.
            var floor = FloorEstimate.At(-1.4f);

            m_Service.UpdateHit(FloorHit(0f, 2f, -1.38f), k_Camera, floor);
            Assert.IsTrue(m_Service.CanPlace);

            m_Service.UpdateHit(FloorHit(0f, 2f, -0.7f), k_Camera, floor);
            Assert.IsFalse(m_Service.CanPlace);
        }

        [Test]
        public void UpdateHits_TableInFrontOfFloor_SkipsTableAndPicksFloor()
        {
            var hits = new[]
            {
                FloorHit(0f, 1.2f, k_FloorY + 0.75f), // table top, closest
                FloorHit(0f, 2.0f),                   // floor behind it
            };

            m_Service.UpdateHits(hits, k_Camera, k_Floor);

            Assert.IsTrue(m_Service.TryConfirm(out var pose));
            Assert.AreEqual(1, m_Service.SelectedHitIndex);
            Assert.AreEqual(hits[1].Position, pose.Position);
        }

        [Test]
        public void TryConfirm_ReturnsExactlyThePreviewPose()
        {
            // The reticle and ghost are drawn at CurrentPose; tapping must place at that same pose,
            // not at the closer elevated hit on the same ray.
            var hits = new[]
            {
                FloorHit(0.2f, 1.1f, k_FloorY + 0.75f),
                FloorHit(0.4f, 3.2f, k_FloorY + 0.02f),
            };
            m_Service.UpdateHits(hits, k_Camera, k_Floor);
            var previewPose = m_Service.CurrentPose;

            Assert.IsTrue(m_Service.TryConfirm(out var placedPose));
            Assert.AreEqual(previewPose.Position, placedPose.Position);
            Assert.AreEqual(previewPose.YawDegrees, placedPose.YawDegrees);
            Assert.AreEqual(hits[1].Position, placedPose.Position);
        }

        [Test]
        public void UpdateHits_SeveralFloorHits_PicksClosest()
        {
            var hits = new[] { FloorHit(0f, 1.5f), FloorHit(0f, 3f) };

            m_Service.UpdateHits(hits, k_Camera, k_Floor);

            Assert.AreEqual(0, m_Service.SelectedHitIndex);
        }

        [Test]
        public void UpdateHits_OnlyElevatedHits_CannotPlace()
        {
            var hits = new[] { FloorHit(0f, 1.2f, 0.75f), FloorHit(0f, 2f, 0.5f) };

            m_Service.UpdateHits(hits, k_Camera, k_Floor);

            Assert.IsFalse(m_Service.CanPlace);
            Assert.AreEqual(-1, m_Service.SelectedHitIndex);
        }

        [Test]
        public void UpdateHits_Empty_CannotPlace()
        {
            m_Service.UpdateHits(new SurfaceHit[0], k_Camera, k_Floor);

            Assert.IsFalse(m_Service.CanPlace);
        }

        [Test]
        public void UpdateHits_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => m_Service.UpdateHits(null, k_Camera, k_Floor));
        }

        [Test]
        public void Constructor_CustomTolerance_IsApplied()
        {
            var service = new PlacementService(0.05f);

            service.UpdateHit(FloorHit(0f, 2f, 0.1f), k_Camera, k_Floor);

            Assert.AreEqual(0.05f, service.FloorTolerance);
            Assert.IsFalse(service.CanPlace);
        }

        [Test]
        public void Constructor_NegativeTolerance_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlacementService(-0.1f));
        }

        [Test]
        public void UpdateHit_CameraBehindHit_ProductFacesCamera()
        {
            // Hit is 2m in front of the camera (+Z), so the product must turn around to face it.
            m_Service.UpdateHit(FloorHit(0f, 2f), k_Camera, k_Floor);

            Assert.AreEqual(180f, m_Service.CurrentPose.YawDegrees, k_Tolerance);
        }

        [TestCase(1f, 0f, 90f)]
        [TestCase(0f, 1f, 0f)]
        [TestCase(-1f, 0f, 270f)]
        [TestCase(0f, -1f, 180f)]
        [TestCase(1f, 1f, 45f)]
        public void ComputeYawTowards_ReturnsYawInZeroTo360(float targetX, float targetZ, float expectedYaw)
        {
            var yaw = PlacementService.ComputeYawTowards(Float3.Zero, new Float3(targetX, 5f, targetZ), 0f);

            Assert.AreEqual(expectedYaw, yaw, k_Tolerance);
        }

        [Test]
        public void ComputeYawTowards_TargetDirectlyAbove_ReturnsFallback()
        {
            var yaw = PlacementService.ComputeYawTowards(Float3.Zero, new Float3(0f, 2f, 0f), 42f);

            Assert.AreEqual(42f, yaw, k_Tolerance);
        }

        [Test]
        public void UpdateHit_CameraDirectlyAbove_KeepsPreviousYaw()
        {
            m_Service.UpdateHit(FloorHit(1f, 0f), Float3.Zero, k_Floor);
            var previousYaw = m_Service.CurrentPose.YawDegrees;

            m_Service.UpdateHit(FloorHit(0f, 0f), new Float3(0f, 1.5f, 0f), k_Floor);

            Assert.AreEqual(previousYaw, m_Service.CurrentPose.YawDegrees, k_Tolerance);
        }
    }
}
