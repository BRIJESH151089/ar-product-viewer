using System;

namespace ARProductViewer.Core
{
    /// <summary>
    /// Which placed item, if any, is selected for manipulation. Items are identified by an integer handle
    /// assigned by whoever owns them. At most one item is selected at a time.
    /// </summary>
    public sealed class PlacedItemSelection
    {
        /// <summary>Handle of the selected item, or null when nothing is selected.</summary>
        public int? SelectedId { get; private set; }

        public bool HasSelection => SelectedId.HasValue;

        /// <summary>Raised with the new selection (null when deselected) whenever it changes.</summary>
        public event Action<int?> SelectionChanged;

        public bool IsSelected(int id) => SelectedId == id;

        /// <summary>Selects <paramref name="id"/>, replacing any current selection.</summary>
        public void Select(int id) => Set(id);

        public void Deselect() => Set(null);

        /// <summary>
        /// Tap on a placed item: selects it, or deselects it if it is already selected.
        /// </summary>
        public void ToggleSelect(int id) => Set(IsSelected(id) ? (int?)null : id);

        /// <summary>Call when an item is removed; clears the selection if it was that item.</summary>
        public void NotifyRemoved(int id)
        {
            if (IsSelected(id))
                Set(null);
        }

        void Set(int? id)
        {
            if (SelectedId == id)
                return;

            SelectedId = id;
            SelectionChanged?.Invoke(id);
        }
    }
}
