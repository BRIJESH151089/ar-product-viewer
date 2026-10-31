using System;

namespace ARProductViewer.Core
{
    /// <summary>
    /// Tracks which catalog product is currently selected for placement.
    /// </summary>
    public sealed class ProductSelectionService
    {
        readonly ProductCatalog m_Catalog;

        /// <summary>The selected product, or null when the catalog is empty.</summary>
        public ProductInfo Selected { get; private set; }

        /// <summary>Raised with the newly selected product whenever the selection changes.</summary>
        public event Action<ProductInfo> SelectionChanged;

        /// <summary>
        /// Starts with the first catalog product selected, or nothing when the catalog is empty.
        /// </summary>
        public ProductSelectionService(ProductCatalog catalog)
        {
            m_Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Selected = catalog.Count > 0 ? catalog.Products[0] : null;
        }

        /// <summary>
        /// Selects the product with <paramref name="id"/>. Returns false and keeps the current
        /// selection when the id is not in the catalog. Re-selecting the current product is a no-op.
        /// </summary>
        public bool Select(string id)
        {
            if (!m_Catalog.TryGet(id, out var product))
                return false;

            if (product == Selected)
                return true;

            Selected = product;
            SelectionChanged?.Invoke(product);
            return true;
        }

        public bool IsSelected(string id) => Selected != null && Selected.Id == id;
    }
}
