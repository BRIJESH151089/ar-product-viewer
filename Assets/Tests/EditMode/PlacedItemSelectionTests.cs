using System.Collections.Generic;
using ARProductViewer.Core;
using NUnit.Framework;

namespace ARProductViewer.Tests
{
    public class PlacedItemSelectionTests
    {
        PlacedItemSelection m_Selection;
        List<int?> m_Changes;

        [SetUp]
        public void SetUp()
        {
            m_Selection = new PlacedItemSelection();
            m_Changes = new List<int?>();
            m_Selection.SelectionChanged += m_Changes.Add;
        }

        [Test]
        public void NewSelection_IsEmpty()
        {
            Assert.IsFalse(m_Selection.HasSelection);
            Assert.IsNull(m_Selection.SelectedId);
        }

        [Test]
        public void Select_SelectsItemAndNotifies()
        {
            m_Selection.Select(3);

            Assert.IsTrue(m_Selection.IsSelected(3));
            CollectionAssert.AreEqual(new int?[] { 3 }, m_Changes);
        }

        [Test]
        public void Select_SameItemTwice_NotifiesOnce()
        {
            m_Selection.Select(3);
            m_Selection.Select(3);

            Assert.AreEqual(1, m_Changes.Count);
        }

        [Test]
        public void Select_OtherItem_SwitchesSoOnlyOneIsSelected()
        {
            m_Selection.Select(3);
            m_Selection.Select(7);

            Assert.IsTrue(m_Selection.IsSelected(7));
            Assert.IsFalse(m_Selection.IsSelected(3));
            CollectionAssert.AreEqual(new int?[] { 3, 7 }, m_Changes);
        }

        [Test]
        public void Deselect_ClearsSelectionAndNotifies()
        {
            m_Selection.Select(3);
            m_Selection.Deselect();

            Assert.IsFalse(m_Selection.HasSelection);
            CollectionAssert.AreEqual(new int?[] { 3, null }, m_Changes);
        }

        [Test]
        public void Deselect_WhenNothingSelected_DoesNotNotify()
        {
            m_Selection.Deselect();

            Assert.IsEmpty(m_Changes);
        }

        [Test]
        public void ToggleSelect_UnselectedItem_SelectsIt()
        {
            m_Selection.ToggleSelect(5);

            Assert.IsTrue(m_Selection.IsSelected(5));
        }

        [Test]
        public void ToggleSelect_SelectedItem_DeselectsIt()
        {
            m_Selection.Select(5);
            m_Selection.ToggleSelect(5);

            Assert.IsFalse(m_Selection.HasSelection);
        }

        [Test]
        public void ToggleSelect_DifferentItem_SwitchesSelection()
        {
            m_Selection.Select(5);
            m_Selection.ToggleSelect(6);

            Assert.IsTrue(m_Selection.IsSelected(6));
        }

        [Test]
        public void NotifyRemoved_SelectedItem_Deselects()
        {
            m_Selection.Select(5);
            m_Selection.NotifyRemoved(5);

            Assert.IsFalse(m_Selection.HasSelection);
        }

        [Test]
        public void NotifyRemoved_OtherItem_KeepsSelection()
        {
            m_Selection.Select(5);
            m_Selection.NotifyRemoved(9);

            Assert.IsTrue(m_Selection.IsSelected(5));
        }
    }
}
