namespace ARProductViewer.Core
{
    /// <summary>
    /// Engine-agnostic rules for deciding whether a product can be placed on a detected surface.
    /// </summary>
    public static class PlacementValidator
    {
        /// <summary>
        /// Returns true when a surface of the given size can hold a product footprint of the given size.
        /// All values are in metres.
        /// </summary>
        public static bool FitsOnSurface(float surfaceWidth, float surfaceDepth, float footprintWidth, float footprintDepth)
        {
            if (footprintWidth <= 0f || footprintDepth <= 0f)
                return false;

            return surfaceWidth >= footprintWidth && surfaceDepth >= footprintDepth;
        }
    }
}
