using System;
using ARProductViewer.Core;
using NUnit.Framework;

namespace ARProductViewer.Tests
{
    public class ProductCatalogTests
    {
        static ProductCatalog CreateCatalog() => new ProductCatalog(new[]
        {
            new ProductInfo("chair", "Chair"),
            new ProductInfo("table", "Table"),
            new ProductInfo("sofa", "Sofa"),
        });

        [Test]
        public void Products_KeepInsertionOrder()
        {
            var catalog = CreateCatalog();

            Assert.AreEqual(3, catalog.Count);
            CollectionAssert.AreEqual(new[] { "chair", "table", "sofa" }, new[]
            {
                catalog.Products[0].Id, catalog.Products[1].Id, catalog.Products[2].Id,
            });
        }

        [Test]
        public void TryGet_KnownId_ReturnsProduct()
        {
            var catalog = CreateCatalog();

            Assert.IsTrue(catalog.TryGet("table", out var product));
            Assert.AreEqual("Table", product.DisplayName);
        }

        [TestCase("lamp")]
        [TestCase("CHAIR")]
        [TestCase(null)]
        public void TryGet_UnknownId_ReturnsFalse(string id)
        {
            var catalog = CreateCatalog();

            Assert.IsFalse(catalog.TryGet(id, out var product));
            Assert.IsNull(product);
            Assert.IsFalse(catalog.Contains(id));
        }

        static ProductCatalog CreateFiveProductCatalog() => new ProductCatalog(new[]
        {
            new ProductInfo("chair", "Chair"),
            new ProductInfo("table", "Table"),
            new ProductInfo("sofa", "Sofa"),
            new ProductInfo("potted_plant", "Potted Plant"),
            new ProductInfo("tall_plant", "Tall Plant"),
        });

        [Test]
        public void FiveProducts_KeepOrderAndFindPlants()
        {
            var catalog = CreateFiveProductCatalog();

            Assert.AreEqual(5, catalog.Count);
            Assert.AreEqual("tall_plant", catalog.Products[4].Id);
            Assert.IsTrue(catalog.TryGet("potted_plant", out var potted));
            Assert.AreEqual("Potted Plant", potted.DisplayName);
            Assert.IsTrue(catalog.Contains("tall_plant"));
        }

        [Test]
        public void FiveProducts_SelectionCanReachEveryProduct()
        {
            var catalog = CreateFiveProductCatalog();
            var selection = new ProductSelectionService(catalog);

            foreach (var product in catalog.Products)
            {
                Assert.IsTrue(selection.Select(product.Id));
                Assert.AreEqual(product.Id, selection.Selected.Id);
            }

            Assert.IsTrue(selection.Select("chair"));
            Assert.AreEqual("chair", selection.Selected.Id);
        }

        [Test]
        public void Constructor_DuplicateId_Throws()
        {
            Assert.Throws<ArgumentException>(() => new ProductCatalog(new[]
            {
                new ProductInfo("chair", "Chair"),
                new ProductInfo("chair", "Another Chair"),
            }));
        }

        [Test]
        public void Constructor_NullProduct_Throws()
        {
            Assert.Throws<ArgumentException>(() => new ProductCatalog(new ProductInfo[] { null }));
        }

        [Test]
        public void Constructor_NullSequence_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ProductCatalog(null));
        }

        [TestCase("", "Chair")]
        [TestCase("chair", " ")]
        [TestCase(null, "Chair")]
        public void ProductInfo_EmptyIdOrName_Throws(string id, string displayName)
        {
            Assert.Throws<ArgumentException>(() => new ProductInfo(id, displayName));
        }
    }
}
