using ARProductViewer.Core;
using NUnit.Framework;

namespace ARProductViewer.Tests
{
    public class PlacementPreviewRulesTests
    {
        [Test]
        public void PlacementActiveOverFloorNotEditing_ShowsPreviewAndPlaces()
        {
            Assert.IsTrue(PlacementPreviewRules.ShouldShowPreview(true, true, false));
            Assert.IsTrue(PlacementPreviewRules.ShouldPlaceOnFloorTap(true, true, false));
        }

        [Test]
        public void ViewMode_HidesPreviewAndNeverPlaces()
        {
            Assert.IsFalse(PlacementPreviewRules.ShouldShowPreview(false, true, false));
            Assert.IsFalse(PlacementPreviewRules.ShouldPlaceOnFloorTap(false, true, false));
        }

        [Test]
        public void EditingPlacedPiece_HidesPreviewAndDoesNotPlace()
        {
            Assert.IsFalse(PlacementPreviewRules.ShouldShowPreview(true, true, true));
            Assert.IsFalse(PlacementPreviewRules.ShouldPlaceOnFloorTap(true, true, true));
        }

        [Test]
        public void NotOverFloor_HidesPreview()
        {
            Assert.IsFalse(PlacementPreviewRules.ShouldShowPreview(true, false, false));
            Assert.IsFalse(PlacementPreviewRules.ShouldPlaceOnFloorTap(true, false, false));
        }

        [TestCase(false, false, false)]
        [TestCase(false, false, true)]
        [TestCase(false, true, true)]
        [TestCase(true, false, true)]
        public void AnyMissingCondition_HidesPreview(bool placementActive, bool hasFloorPose, bool editing)
        {
            Assert.IsFalse(PlacementPreviewRules.ShouldShowPreview(placementActive, hasFloorPose, editing));
        }
    }
}
