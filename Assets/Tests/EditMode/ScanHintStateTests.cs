using System.Collections.Generic;
using ARProductViewer.Core;
using NUnit.Framework;

namespace ARProductViewer.Tests
{
    public class ScanHintStateTests
    {
        ScanHintState m_State;
        List<bool> m_Changes;

        [SetUp]
        public void SetUp()
        {
            m_State = new ScanHintState();
            m_Changes = new List<bool>();
            m_State.VisibilityChanged += m_Changes.Add;
        }

        [Test]
        public void NewState_IsVisible()
        {
            Assert.IsTrue(m_State.IsVisible);
            Assert.IsFalse(m_State.HasFoundFloor);
        }

        [Test]
        public void NoFloor_StaysVisibleWithoutNotifying()
        {
            m_State.Update(false, false);

            Assert.IsTrue(m_State.IsVisible);
            Assert.IsEmpty(m_Changes);
        }

        [Test]
        public void FloorDetectedButReticleNotYetShown_StaysVisible()
        {
            m_State.Update(true, false);

            Assert.IsTrue(m_State.IsVisible);
        }

        [Test]
        public void FloorAndReticle_HidesAndNotifiesOnce()
        {
            m_State.Update(true, true);
            m_State.Update(true, true);

            Assert.IsFalse(m_State.IsVisible);
            CollectionAssert.AreEqual(new[] { false }, m_Changes);
        }

        [Test]
        public void AfterFound_PointingAwayFromFloor_StaysHidden()
        {
            m_State.Update(true, true);
            m_State.Update(true, false);

            Assert.IsFalse(m_State.IsVisible);
        }

        [Test]
        public void AfterFound_FloorLost_ShowsAgain()
        {
            m_State.Update(true, true);
            m_State.Update(false, false);

            Assert.IsTrue(m_State.IsVisible);
            CollectionAssert.AreEqual(new[] { false, true }, m_Changes);
        }

        [Test]
        public void FloorLostThenFoundAgain_HidesAgain()
        {
            m_State.Update(true, true);
            m_State.Update(false, false);
            m_State.Update(true, true);

            Assert.IsFalse(m_State.IsVisible);
        }
    }
}
