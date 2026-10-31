using ARProductViewer.Core;
using NUnit.Framework;

namespace ARProductViewer.Tests
{
    public class FloorDragTests
    {
        const float k_FloorY = -1.4f;

        [Test]
        public void Update_KeepsInitialOffsetBetweenFingerAndItem()
        {
            var drag = new FloorDrag();
            drag.Begin(new Float3(1f, k_FloorY, 2f), new Float3(1.2f, k_FloorY, 2.1f));

            var moved = drag.Update(new Float3(2.2f, k_FloorY, 3.1f));

            Assert.AreEqual(2f, moved.X, 0.0001f);
            Assert.AreEqual(3f, moved.Z, 0.0001f);
        }

        [Test]
        public void Update_FingerPointAtDifferentHeight_ItemStaysOnFloor()
        {
            var drag = new FloorDrag();
            drag.Begin(new Float3(0f, k_FloorY, 2f), new Float3(0f, k_FloorY, 2f));

            var moved = drag.Update(new Float3(0.5f, k_FloorY + 0.3f, 2.5f));

            Assert.IsTrue(ManipulationMath.IsOnFloor(moved, k_FloorY));
            Assert.AreEqual(k_FloorY, drag.FloorHeight);
        }

        [Test]
        public void BeginAndEnd_ToggleIsActive()
        {
            var drag = new FloorDrag();
            Assert.IsFalse(drag.IsActive);

            drag.Begin(Float3.Zero, Float3.Zero);
            Assert.IsTrue(drag.IsActive);

            drag.End();
            Assert.IsFalse(drag.IsActive);
        }
    }
}
