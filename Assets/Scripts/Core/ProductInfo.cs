using System;

namespace ARProductViewer.Core
{
    /// <summary>
    /// Engine-agnostic description of a product the user can place.
    /// </summary>
    public sealed class ProductInfo
    {
        /// <summary>Stable identifier, unique within a catalog.</summary>
        public string Id { get; }

        /// <summary>Name shown to the user.</summary>
        public string DisplayName { get; }

        public ProductInfo(string id, string displayName)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Product id must not be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Product display name must not be empty.", nameof(displayName));

            Id = id;
            DisplayName = displayName;
        }

        public override string ToString() => $"{DisplayName} ({Id})";
    }
}
