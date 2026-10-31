using System;
using System.Collections.Generic;
using ARProductViewer.Core;
using UnityEngine;

namespace ARProductViewer.App
{
    /// <summary>
    /// Authoring data for the product catalog: maps each product id to its prefab and thumbnail.
    /// </summary>
    [CreateAssetMenu(fileName = "ProductCatalog", menuName = "AR Product Viewer/Product Catalog")]
    public sealed class ProductCatalogAsset : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField, Tooltip("Stable id, unique within the catalog.")]
            string m_Id;

            [SerializeField]
            string m_DisplayName;

            [SerializeField, Tooltip("Pivot at the bottom centre, front facing +Z, sized in metres.")]
            GameObject m_Prefab;

            [SerializeField]
            Sprite m_Thumbnail;

            public string Id => m_Id;
            public string DisplayName => m_DisplayName;
            public GameObject Prefab => m_Prefab;
            public Sprite Thumbnail => m_Thumbnail;

            public Entry(string id, string displayName, GameObject prefab, Sprite thumbnail)
            {
                m_Id = id;
                m_DisplayName = displayName;
                m_Prefab = prefab;
                m_Thumbnail = thumbnail;
            }
        }

        [SerializeField]
        List<Entry> m_Entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => m_Entries;

        /// <summary>Builds the engine-agnostic catalog. Throws if ids are missing or duplicated.</summary>
        public ProductCatalog CreateCatalog()
        {
            var products = new List<ProductInfo>(m_Entries.Count);
            foreach (var entry in m_Entries)
                products.Add(new ProductInfo(entry.Id, entry.DisplayName));

            return new ProductCatalog(products);
        }

        public bool TryGetEntry(string id, out Entry entry)
        {
            entry = m_Entries.Find(e => e.Id == id);
            return entry != null;
        }
    }
}
