namespace ARProductViewer.Core
{
    /// <summary>
    /// When the placement preview (ghost + reticle) is shown, and when a floor tap places a new product.
    /// </summary>
    public static class PlacementPreviewRules
    {
        /// <param name="placementActive">A catalog product is selected (not in view mode).</param>
        /// <param name="hasFloorPose">The screen centre is over a floor-level plane.</param>
        /// <param name="editingPlacedItem">A placed piece is selected or being manipulated.</param>
        public static bool ShouldShowPreview(bool placementActive, bool hasFloorPose, bool editingPlacedItem)
        {
            return placementActive && hasFloorPose && !editingPlacedItem;
        }

        /// <summary>
        /// A tap on empty floor places a product only when the preview is visible, so nothing is ever placed
        /// without the user having seen where it will go.
        /// </summary>
        public static bool ShouldPlaceOnFloorTap(bool placementActive, bool hasFloorPose, bool editingPlacedItem)
        {
            return ShouldShowPreview(placementActive, hasFloorPose, editingPlacedItem);
        }
    }
}
