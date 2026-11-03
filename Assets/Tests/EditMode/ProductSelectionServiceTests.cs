using System.Collections.Generic;
using ARProductViewer.Core;
using NUnit.Framework;

namespace ARProductViewer.Tests
{
    public class ProductSelectionServiceTests
    {
        ProductSelectionService m_Selection;
        List<ProductInfo> m_Changes;

        [SetUp]
        public void SetUp()
        {
            var catalog = new ProductCatalog(new[]
            {
                new ProductInfo("chair", "Chair"),
                new ProductInfo("table", "Table"),
            });
            m_Selection = new ProductSelectionService(catalog);
            m_Changes = new List<ProductInfo>();
            m_Selection.SelectionChanged += m_Changes.Add;
        }

        [Test]
        public void NewService_SelectsFirstProduct()
        {
            Assert.AreEqual("chair", m_Selection.Selected.Id);
            Assert.IsTrue(m_Selection.IsSelected("chair"));
        }

        [Test]
        public void NewService_EmptyCatalog_SelectsNothing()
        {
            var selection = new ProductSelectionService(new ProductCatalog(new ProductInfo[0]));

            Assert.IsNull(selection.Selected);
            Assert.IsFalse(selection.IsSelected("chair"));
        }

        [Test]
        public void Select_KnownProduct_ChangesSelectionAndNotifiesOnce()
        {
            Assert.IsTrue(m_Selection.Select("table"));

            Assert.AreEqual("table", m_Selection.Selected.Id);
            Assert.IsFalse(m_Selection.IsSelected("chair"));
            Assert.AreEqual(1, m_Changes.Count);
            Assert.AreEqual("table", m_Changes[0].Id);
        }

        [Test]
        public void Select_UnknownProduct_KeepsSelectionAndDoesNotNotify()
        {
            Assert.IsFalse(m_Selection.Select("lamp"));

            Assert.AreEqual("chair", m_Selection.Selected.Id);
            Assert.IsEmpty(m_Changes);
        }

        [Test]
        public void Select_AlreadySelectedProduct_DoesNotNotify()
        {
            Assert.IsTrue(m_Selection.Select("chair"));

            Assert.IsEmpty(m_Changes);
        }

        [Test]
        public void NewService_StartsInPlacementMode()
        {
            Assert.IsTrue(m_Selection.IsPlacementActive);
        }

        [Test]
        public void ToggleSelect_SelectedProduct_EntersViewMode()
        {
            Assert.IsTrue(m_Selection.ToggleSelect("chair"));

            Assert.IsNull(m_Selection.Selected);
            Assert.IsFalse(m_Selection.IsPlacementActive);
            Assert.IsFalse(m_Selection.IsSelected("chair"));
            Assert.AreEqual(1, m_Changes.Count);
            Assert.IsNull(m_Changes[0]);
        }

        [Test]
        public void ToggleSelect_InViewMode_ReentersPlacementWithThatProduct()
        {
            m_Selection.ToggleSelect("chair");

            Assert.IsTrue(m_Selection.ToggleSelect("table"));

            Assert.IsTrue(m_Selection.IsPlacementActive);
            Assert.AreEqual("table", m_Selection.Selected.Id);
        }

        [Test]
        public void ToggleSelect_OtherProduct_SwitchesWithoutLeavingPlacement()
        {
            m_Selection.ToggleSelect("table");

            Assert.IsTrue(m_Selection.IsPlacementActive);
            Assert.AreEqual("table", m_Selection.Selected.Id);
            Assert.AreEqual(1, m_Changes.Count);
        }

        [Test]
        public void ToggleSelect_UnknownProduct_ChangesNothing()
        {
            Assert.IsFalse(m_Selection.ToggleSelect("lamp"));

            Assert.AreEqual("chair", m_Selection.Selected.Id);
            Assert.IsEmpty(m_Changes);
        }

        [Test]
        public void Deselect_TwiceInViewMode_NotifiesOnce()
        {
            m_Selection.Deselect();
            m_Selection.Deselect();

            Assert.IsFalse(m_Selection.IsPlacementActive);
            Assert.AreEqual(1, m_Changes.Count);
        }

        [Test]
        public void Select_BackAndForth_NotifiesEachChange()
        {
            m_Selection.Select("table");
            m_Selection.Select("chair");

            Assert.AreEqual(2, m_Changes.Count);
            Assert.AreEqual("chair", m_Selection.Selected.Id);
        }
    }
}
