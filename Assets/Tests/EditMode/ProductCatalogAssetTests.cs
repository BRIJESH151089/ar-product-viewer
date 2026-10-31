using System.Linq;
using ARProductViewer.App;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ARProductViewer.Tests
{
    /// <summary>
    /// Guards the shipped catalog content: every product the picker shows must be placeable.
    /// </summary>
    public class ProductCatalogAssetTests
    {
        const string k_CatalogPath = "Assets/Data/ProductCatalog.asset";
        const float k_PivotTolerance = 0.001f;

        static readonly string[] k_ExpectedIds = { "chair", "table", "sofa", "potted_plant", "tall_plant" };

        ProductCatalogAsset m_Catalog;

        [SetUp]
        public void SetUp()
        {
            m_Catalog = AssetDatabase.LoadAssetAtPath<ProductCatalogAsset>(k_CatalogPath);
            Assert.IsNotNull(m_Catalog, $"Catalog asset not found at {k_CatalogPath}");
        }

        [Test]
        public void Catalog_HasFiveProductsInPickerOrder()
        {
            CollectionAssert.AreEqual(k_ExpectedIds, m_Catalog.Entries.Select(e => e.Id).ToArray());
        }

        [Test]
        public void Catalog_BuildsCoreCatalogWithFiveProducts()
        {
            var catalog = m_Catalog.CreateCatalog();

            Assert.AreEqual(5, catalog.Count);
            foreach (var id in k_ExpectedIds)
                Assert.IsTrue(catalog.Contains(id), id);
        }

        [Test]
        public void Catalog_EveryEntryHasNamePrefabAndThumbnail()
        {
            foreach (var entry in m_Catalog.Entries)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(entry.DisplayName), $"{entry.Id}: display name");
                Assert.IsNotNull(entry.Prefab, $"{entry.Id}: prefab");
                Assert.IsNotNull(entry.Thumbnail, $"{entry.Id}: thumbnail");
            }
        }

        [Test]
        public void Catalog_EveryPrefabHasRootBoxColliderWithBottomAtPivot()
        {
            foreach (var entry in m_Catalog.Entries)
            {
                var box = entry.Prefab.GetComponent<BoxCollider>();
                Assert.IsNotNull(box, $"{entry.Id}: root BoxCollider for tap selection");

                var bottom = box.center.y - box.size.y * 0.5f;
                Assert.AreEqual(0f, bottom, k_PivotTolerance, $"{entry.Id}: collider bottom must sit on the pivot (floor)");
                Assert.AreEqual(0f, box.center.x, k_PivotTolerance, $"{entry.Id}: footprint centred on pivot (x)");
                Assert.AreEqual(0f, box.center.z, k_PivotTolerance, $"{entry.Id}: footprint centred on pivot (z)");
                Assert.AreEqual(Vector3.one, entry.Prefab.transform.localScale, $"{entry.Id}: root scale must be 1 (real size)");
            }
        }
    }
}
