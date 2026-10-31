using System;
using ARProductViewer.Core;
using NUnit.Framework;

namespace ARProductViewer.Tests
{
    public class AnchorFollowerTests
    {
        const float k_Tolerance = 0.0001f;

        // Typical session: floor ~1.4m below the session origin, reticle 2.5m ahead.
        static readonly PlacementPose k_Intended = new PlacementPose(new Float3(0.4f, -1.4f, 2.5f), 200f);
        static readonly PlacementPose k_SessionOrigin = new PlacementPose(Float3.Zero, 0f);

        static void AssertPose(PlacementPose expected, PlacementPose actual)
        {
            Assert.AreEqual(expected.Position.X, actual.Position.X, k_Tolerance, "X");
            Assert.AreEqual(expected.Position.Y, actual.Position.Y, k_Tolerance, "Y");
            Assert.AreEqual(expected.Position.Z, actual.Position.Z, k_Tolerance, "Z");
            Assert.AreEqual(expected.YawDegrees, actual.YawDegrees, k_Tolerance, "Yaw");
        }

        [Test]
        public void NewFollower_StartsExactlyAtIntendedPose()
        {
            var follower = new AnchorFollower(k_Intended);

            AssertPose(k_Intended, follower.Pose);
            Assert.IsFalse(follower.IsLocked);
        }

        [Test]
        public void Update_AnchorNotTracking_KeepsIntendedPose()
        {
            var follower = new AnchorFollower(k_Intended);

            var pose = follower.Update(false, k_SessionOrigin);

            AssertPose(k_Intended, pose);
            Assert.IsFalse(follower.IsLocked);
        }

        [Test]
        public void Update_TrackingAnchorReportedAtSessionOrigin_KeepsIntendedPose()
        {
            // The on-device bug: an anchor reported at the origin dragged the product up to the camera.
            var follower = new AnchorFollower(k_Intended);

            var pose = follower.Update(true, k_SessionOrigin);

            AssertPose(k_Intended, pose);
            Assert.IsFalse(follower.IsLocked);
        }

        [Test]
        public void Update_AnchorAtIntendedPose_LocksWithoutMovingProduct()
        {
            var follower = new AnchorFollower(k_Intended);

            var pose = follower.Update(true, k_Intended);

            Assert.IsTrue(follower.IsLocked);
            AssertPose(k_Intended, pose);
        }

        [Test]
        public void Update_AnchorSlightlyOffWhenLocking_ProductStaysAtIntendedPose()
        {
            var follower = new AnchorFollower(k_Intended);
            var anchor = new PlacementPose(k_Intended.Position + new Float3(0.1f, 0.02f, -0.05f), 185f);

            var pose = follower.Update(true, anchor);

            Assert.IsTrue(follower.IsLocked);
            AssertPose(k_Intended, pose);
        }

        [Test]
        public void Update_AfterLock_FollowsSmallAnchorCorrection()
        {
            var follower = new AnchorFollower(k_Intended);
            follower.Update(true, k_Intended);
            var correction = new Float3(0.03f, -0.01f, 0.02f);

            var pose = follower.Update(true, new PlacementPose(k_Intended.Position + correction, k_Intended.YawDegrees));

            AssertPose(new PlacementPose(k_Intended.Position + correction, k_Intended.YawDegrees), pose);
        }

        [Test]
        public void Update_AfterLock_AnchorRotation_CarriesOffsetAroundAnchor()
        {
            // Anchor at the origin facing +Z, product 1m ahead of it facing +Z.
            var follower = new AnchorFollower(new PlacementPose(new Float3(0f, 0f, 1f), 0f), lockDistance: 2f, maxJump: 5f);
            follower.Update(true, k_SessionOrigin);

            // Anchor turns 90 degrees: +Z now points to +X, so the product should swing to +X and turn too.
            var pose = follower.Update(true, new PlacementPose(Float3.Zero, 90f));

            AssertPose(new PlacementPose(new Float3(1f, 0f, 0f), 90f), pose);
        }

        [Test]
        public void Update_AfterLock_ImplausibleJump_IsIgnored()
        {
            var follower = new AnchorFollower(k_Intended);
            follower.Update(true, k_Intended);

            var pose = follower.Update(true, k_SessionOrigin);

            AssertPose(k_Intended, pose);
        }

        [Test]
        public void Update_AfterLock_AnchorStopsTracking_KeepsLastPose()
        {
            var follower = new AnchorFollower(k_Intended);
            follower.Update(true, k_Intended);
            var corrected = new PlacementPose(k_Intended.Position + new Float3(0.02f, 0f, 0f), k_Intended.YawDegrees);
            follower.Update(true, corrected);

            var pose = follower.Update(false, k_SessionOrigin);

            AssertPose(corrected, pose);
        }

        [TestCase(0f, 0f, 1f, 90f, 1f, 0f)]
        [TestCase(1f, 0f, 0f, 90f, 0f, -1f)]
        [TestCase(0f, 0f, 1f, 180f, 0f, -1f)]
        [TestCase(0f, 0f, 1f, -90f, -1f, 0f)]
        public void RotateYaw_MatchesUnityYRotation(float x, float y, float z, float yaw, float expectedX, float expectedZ)
        {
            var rotated = AnchorFollower.RotateYaw(new Float3(x, y, z), yaw);

            Assert.AreEqual(expectedX, rotated.X, k_Tolerance);
            Assert.AreEqual(y, rotated.Y, k_Tolerance);
            Assert.AreEqual(expectedZ, rotated.Z, k_Tolerance);
        }

        [Test]
        public void Constructor_InvalidSettings_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AnchorFollower(k_Intended, lockDistance: -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AnchorFollower(k_Intended, maxJump: 0f));
        }
    }
}
