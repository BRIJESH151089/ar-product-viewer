namespace ARProductViewer.Core
{
    /// <summary>
    /// Moves a product along the floor with a finger, keeping the horizontal offset between where the finger
    /// first touched and the product's pivot so the product does not jump to the finger. Finger points can be
    /// taken on any horizontal plane (e.g. at the height the finger grabbed the product); only their X/Z are
    /// used, and the product always stays at the floor height it started at.
    /// </summary>
    public sealed class FloorDrag
    {
        Float3 m_Offset;

        /// <summary>Height of the floor the product is dragged along.</summary>
        public float FloorHeight { get; private set; }

        public bool IsActive { get; private set; }

        /// <param name="itemPosition">The product's pivot when the drag starts.</param>
        /// <param name="fingerPoint">Where the finger's ray meets the drag plane when the drag starts.</param>
        public void Begin(Float3 itemPosition, Float3 fingerPoint)
        {
            FloorHeight = itemPosition.Y;
            m_Offset = new Float3(itemPosition.X - fingerPoint.X, 0f, itemPosition.Z - fingerPoint.Z);
            IsActive = true;
        }

        /// <summary>Returns the product position for the finger's current point on the drag plane, always at <see cref="FloorHeight"/>.</summary>
        public Float3 Update(Float3 fingerPoint)
        {
            return ManipulationMath.KeepOnFloor(fingerPoint + m_Offset, FloorHeight);
        }

        public void End() => IsActive = false;
    }
}
