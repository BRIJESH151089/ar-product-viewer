using ARProductViewer.Core;
using NUnit.Framework;

namespace ARProductViewer.Tests
{
    public class PlacementServiceTests
    {
        const float k_Tolerance = 0.001f;

        static readonly Float3 k_Camera = new Float3(0f, 1.5f, 0f);

        PlacementService m_Service;

        [SetUp]
        public void SetUp()
        {
            m_Service = new PlacementService();
        }

        [Test]
        public void NewService_CannotPlace()
        {
            Assert.IsFalse(m_Service.CanPlace);
            Assert.IsFalse(m_Service.TryConfirm(out _));
        }

        [Test]
        public void UpdateHit_NoHit_CannotPlace()
        {
            m_Service.UpdateHit(SurfaceHit.None, k_Camera);

            Assert.IsFalse(m_Service.CanPlace);
            Assert.IsFalse(m_Service.TryConfirm(out _));
        }

        [Test]
        public void UpdateHit_HitOnNonHorizontalSurface_CannotPlace()
        {
            m_Service.UpdateHit(new SurfaceHit(true, false, new Float3(0f, 0f, 2f)), k_Camera);

            Assert.IsFalse(m_Service.CanPlace);
        }

        [Test]
        public void UpdateHit_HorizontalPlaneHit_CanPlaceAtHitPosition()
        {
            var hitPosition = new Float3(0.3f, 0f, 2f);

            m_Service.UpdateHit(SurfaceHit.OnHorizontalPlane(hitPosition), k_Camera);

            Assert.IsTrue(m_Service.TryConfirm(out var pose));
            Assert.AreEqual(hitPosition, pose.Position);
        }

        [Test]
        public void UpdateHit_ValidThenNoHit_CannotPlace()
        {
            m_Service.UpdateHit(SurfaceHit.OnHorizontalPlane(new Float3(0f, 0f, 2f)), k_Camera);
            m_Service.UpdateHit(SurfaceHit.None, k_Camera);

            Assert.IsFalse(m_Service.TryConfirm(out _));
        }

        [Test]
        public void UpdateHit_CameraBehindHit_ProductFacesCamera()
        {
            // Hit is 2m in front of the camera (+Z), so the product must turn around to face it.
            m_Service.UpdateHit(SurfaceHit.OnHorizontalPlane(new Float3(0f, 0f, 2f)), k_Camera);

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
            m_Service.UpdateHit(SurfaceHit.OnHorizontalPlane(new Float3(1f, 0f, 0f)), Float3.Zero);
            var previousYaw = m_Service.CurrentPose.YawDegrees;

            m_Service.UpdateHit(SurfaceHit.OnHorizontalPlane(new Float3(0f, 0f, 0f)), new Float3(0f, 1.5f, 0f));

            Assert.AreEqual(previousYaw, m_Service.CurrentPose.YawDegrees, k_Tolerance);
        }
    }
}
