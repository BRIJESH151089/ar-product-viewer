namespace ARProductViewer.Core
{
    /// <summary>
    /// Moves a product along the floor with a finger, keeping the offset between where the finger first
    /// touched and the product's pivot so the product does not jump to the finger.
    /// </summary>
    public sealed class FloorDrag
    {
        Float3 m_Offset;

        /// <summary>Height of the floor the product is dragged along.</summary>
        public float FloorHeight { get; private set; }

        public bool IsActive { get; private set; }

        /// <param name="itemPosition">The product's pivot when the drag starts.</param>
        /// <param name="fingerFloorPoint">Where the finger's ray meets the floor when the drag starts.</param>
        public void Begin(Float3 itemPosition, Float3 fingerFloorPoint)
        {
            FloorHeight = itemPosition.Y;
            m_Offset = new Float3(itemPosition.X - fingerFloorPoint.X, 0f, itemPosition.Z - fingerFloorPoint.Z);
            IsActive = true;
        }

        /// <summary>Returns the product position for the finger's current floor point, always at <see cref="FloorHeight"/>.</summary>
        public Float3 Update(Float3 fingerFloorPoint)
        {
            return ManipulationMath.KeepOnFloor(fingerFloorPoint + m_Offset, FloorHeight);
        }

        public void End() => IsActive = false;
    }
}
