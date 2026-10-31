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
