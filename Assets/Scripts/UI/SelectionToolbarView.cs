using System;
using ARProductViewer.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ARProductViewer.UI
{
    /// <summary>
    /// Deselect and Delete buttons, shown only while a placed piece is selected.
    /// Deselect updates the Core selection directly; Delete is raised as an event because removing
    /// a piece is handled outside the UI layer.
    /// </summary>
    public sealed class SelectionToolbarView : MonoBehaviour
    {
        [SerializeField, Tooltip("Shown while something is selected, hidden otherwise.")]
        GameObject m_Panel;

        [SerializeField]
        Button m_DeselectButton;

        [SerializeField]
        Button m_DeleteButton;

        PlacedItemSelection m_Selection;

        /// <summary>Raised when the user asks to delete the selected piece.</summary>
        public event Action DeleteClicked;

        public void Initialize(PlacedItemSelection selection)
        {
            Unsubscribe();
            m_Selection = selection ?? throw new ArgumentNullException(nameof(selection));
            m_Selection.SelectionChanged += OnSelectionChanged;
            Refresh();
        }

        void Awake()
        {
            m_DeselectButton.onClick.AddListener(OnDeselectClicked);
            m_DeleteButton.onClick.AddListener(OnDeleteClicked);
            m_Panel.SetActive(false);
        }

        void OnDestroy()
        {
            m_DeselectButton.onClick.RemoveListener(OnDeselectClicked);
            m_DeleteButton.onClick.RemoveListener(OnDeleteClicked);
            Unsubscribe();
        }

        void OnSelectionChanged(int? selectedId) => Refresh();

        void Refresh() => m_Panel.SetActive(m_Selection != null && m_Selection.HasSelection);

        void OnDeselectClicked() => m_Selection?.Deselect();

        void OnDeleteClicked() => DeleteClicked?.Invoke();

        void Unsubscribe()
        {
            if (m_Selection != null)
                m_Selection.SelectionChanged -= OnSelectionChanged;
        }
    }
}
