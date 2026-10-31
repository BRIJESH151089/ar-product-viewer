using ARProductViewer.Core;
using UnityEngine;

namespace ARProductViewer.UI
{
    /// <summary>
    /// What the picker needs to show for one product.
    /// </summary>
    public readonly struct ProductPickerItem
    {
        public readonly ProductInfo Product;
        public readonly Sprite Thumbnail;

        public ProductPickerItem(ProductInfo product, Sprite thumbnail)
        {
            Product = product;
            Thumbnail = thumbnail;
        }
    }
}
