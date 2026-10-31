using System.Collections.Generic;
using ARProductViewer.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ARProductViewer.UI
{
    /// <summary>
    /// Row of product buttons. Tapping a button selects that product in the
    /// <see cref="ProductSelectionService"/>; the highlighted button follows the service's selection.
    /// </summary>
    public sealed class ProductPickerView : MonoBehaviour
    {
        [SerializeField, Tooltip("Parent the product buttons are created under.")]
        RectTransform m_Content;

        [SerializeField]
        ProductButton m_ButtonPrefab;

        [SerializeField, Tooltip("Optional. Reset to the start after the buttons are built so the first product is fully visible.")]
        ScrollRect m_ScrollRect;

        readonly List<ProductButton> m_Buttons = new List<ProductButton>();
        ProductSelectionService m_Selection;

        public void Initialize(ProductSelectionService selection, IReadOnlyList<ProductPickerItem> items)
        {
            Unsubscribe();
            ClearButtons();

            m_Selection = selection;
            foreach (var item in items)
            {
                var button = Instantiate(m_ButtonPrefab, m_Content);
                var id = item.Product.Id;
                button.Bind(item.Product, item.Thumbnail, () => m_Selection.Select(id));
                m_Buttons.Add(button);
            }

            m_Selection.SelectionChanged += OnSelectionChanged;
            RefreshSelection();
            ScrollToStart();
        }

        void ScrollToStart()
        {
            if (m_ScrollRect == null)
                return;

            // Lay out the new buttons now so the content width is known before snapping to the start.
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_Content);
            m_ScrollRect.StopMovement();
            m_ScrollRect.horizontalNormalizedPosition = 0f;
        }

        void OnDestroy() => Unsubscribe();

        void OnSelectionChanged(ProductInfo product) => RefreshSelection();

        void RefreshSelection()
        {
            foreach (var button in m_Buttons)
                button.SetSelected(m_Selection.IsSelected(button.ProductId));
        }

        void Unsubscribe()
        {
            if (m_Selection != null)
                m_Selection.SelectionChanged -= OnSelectionChanged;
        }

        void ClearButtons()
        {
            foreach (var button in m_Buttons)
            {
                if (button != null)
                    Destroy(button.gameObject);
            }

            m_Buttons.Clear();
        }
    }
}
