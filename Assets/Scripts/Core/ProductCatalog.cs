using System;
using System.Collections.Generic;

namespace ARProductViewer.Core
{
    /// <summary>
    /// Ordered, read-only set of products with unique ids.
    /// </summary>
    public sealed class ProductCatalog
    {
        readonly List<ProductInfo> m_Products = new List<ProductInfo>();
        readonly Dictionary<string, ProductInfo> m_ById = new Dictionary<string, ProductInfo>(StringComparer.Ordinal);

        /// <summary>Products in display order.</summary>
        public IReadOnlyList<ProductInfo> Products => m_Products;

        public int Count => m_Products.Count;

        public ProductCatalog(IEnumerable<ProductInfo> products)
        {
            if (products == null)
                throw new ArgumentNullException(nameof(products));

            foreach (var product in products)
            {
                if (product == null)
                    throw new ArgumentException("Catalog must not contain null products.", nameof(products));
                if (m_ById.ContainsKey(product.Id))
                    throw new ArgumentException($"Duplicate product id '{product.Id}'.", nameof(products));

                m_ById.Add(product.Id, product);
                m_Products.Add(product);
            }
        }

        public bool Contains(string id) => id != null && m_ById.ContainsKey(id);

        public bool TryGet(string id, out ProductInfo product)
        {
            if (id == null)
            {
                product = null;
                return false;
            }

            return m_ById.TryGetValue(id, out product);
        }
    }
}
