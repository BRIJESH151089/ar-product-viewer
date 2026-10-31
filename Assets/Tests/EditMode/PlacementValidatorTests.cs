using ARProductViewer.Core;
using NUnit.Framework;

namespace ARProductViewer.Tests
{
    public class PlacementValidatorTests
    {
        [Test]
        public void FitsOnSurface_FootprintSmallerThanSurface_ReturnsTrue()
        {
            Assert.IsTrue(PlacementValidator.FitsOnSurface(2f, 1.5f, 0.8f, 0.6f));
        }
    }
}
