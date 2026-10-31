using System.Collections.Generic;
using ARProductViewer.AR;
using ARProductViewer.Core;
using ARProductViewer.UI;
using UnityEngine;

namespace ARProductViewer.App
{
    /// <summary>
    /// Composition root: builds the Core catalog and selection service, feeds the picker UI,
    /// and keeps the AR placement prefab in sync with the selected product.
    /// </summary>
    public sealed class ProductViewerBootstrap : MonoBehaviour
    {
        [SerializeField]
        ProductCatalogAsset m_Catalog;

        [SerializeField]
        ARTapToPlace m_TapToPlace;

        [SerializeField]
        ProductPickerView m_Picker;

        [SerializeField]
        FurnitureInteraction m_Interaction;

        [SerializeField]
        SelectionToolbarView m_SelectionToolbar;

        [SerializeField]
        ScanHintView m_ScanHint;

        ProductSelectionService m_Selection;
        PlacedItemSelection m_PlacedSelection;
        readonly ScanHintState m_ScanHintState = new ScanHintState();

        void Awake()
        {
            // Shared before any input is processed so taps select into the same state the toolbar shows.
            m_PlacedSelection = new PlacedItemSelection();
            m_Interaction.Initialize(m_PlacedSelection);
            m_SelectionToolbar.Initialize(m_PlacedSelection);
            m_SelectionToolbar.DeleteClicked += m_Interaction.DeleteSelected;

            if (m_ScanHint != null)
                m_ScanHint.Initialize(m_ScanHintState);
        }

        void Update()
        {
            m_ScanHintState.Update(m_TapToPlace.Floor.HasFloor, m_TapToPlace.HasPlacementPose);
        }

        void Start()
        {
            var catalog = m_Catalog.CreateCatalog();
            m_Selection = new ProductSelectionService(catalog);

            var items = new List<ProductPickerItem>(catalog.Count);
            foreach (var product in catalog.Products)
            {
                m_Catalog.TryGetEntry(product.Id, out var entry);
                items.Add(new ProductPickerItem(product, entry.Thumbnail));
            }

            m_Picker.Initialize(m_Selection, items);
            m_Selection.SelectionChanged += ApplySelection;
            ApplySelection(m_Selection.Selected);
        }

        void OnDestroy()
        {
            if (m_Selection != null)
                m_Selection.SelectionChanged -= ApplySelection;
            if (m_SelectionToolbar != null && m_Interaction != null)
                m_SelectionToolbar.DeleteClicked -= m_Interaction.DeleteSelected;
        }

        void ApplySelection(ProductInfo product)
        {
            if (product == null || !m_Catalog.TryGetEntry(product.Id, out var entry) || entry.Prefab == null)
            {
                Debug.LogWarning($"{nameof(ProductViewerBootstrap)}: no prefab for product '{product?.Id}'.", this);
                m_TapToPlace.SetProductPrefab(null);
                return;
            }

            m_TapToPlace.SetProductPrefab(entry.Prefab);
        }
    }
}
