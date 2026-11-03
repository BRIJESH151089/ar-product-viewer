using System;

namespace ARProductViewer.Core
{
    /// <summary>
    /// Tracks which catalog product is currently selected for placement. Deselecting leaves placement mode
    /// ("view mode"): nothing is previewed or placed until a product is selected again.
    /// </summary>
    public sealed class ProductSelectionService
    {
        readonly ProductCatalog m_Catalog;

        /// <summary>The selected product, or null in view mode (or when the catalog is empty).</summary>
        public ProductInfo Selected { get; private set; }

        /// <summary>True while a product is selected for placement.</summary>
        public bool IsPlacementActive => Selected != null;

        /// <summary>Raised with the newly selected product (null when deselected) whenever the selection changes.</summary>
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

        /// <summary>Leaves placement mode. No-op when nothing is selected.</summary>
        public void Deselect()
        {
            if (Selected == null)
                return;

            Selected = null;
            SelectionChanged?.Invoke(null);
        }

        /// <summary>
        /// Picker tap: selects <paramref name="id"/>, or deselects it if it is already selected.
        /// Returns false for ids not in the catalog.
        /// </summary>
        public bool ToggleSelect(string id)
        {
            if (!m_Catalog.Contains(id))
                return false;

            if (IsSelected(id))
            {
                Deselect();
                return true;
            }

            return Select(id);
        }

        public bool IsSelected(string id) => Selected != null && Selected.Id == id;
    }
}
