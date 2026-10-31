using System;

namespace ARProductViewer.Core
{
    /// <summary>
    /// Decides when to show the "scan the floor" onboarding hint: shown until a floor has been detected and
    /// the placement reticle has appeared; hidden after that, even while the user points away from the floor;
    /// shown again only if the floor is lost entirely (e.g. tracking was reset).
    /// </summary>
    public sealed class ScanHintState
    {
        public bool IsVisible { get; private set; } = true;

        /// <summary>True once a floor was found and the reticle appeared on it.</summary>
        public bool HasFoundFloor { get; private set; }

        /// <summary>Raised with the new visibility whenever it changes.</summary>
        public event Action<bool> VisibilityChanged;

        public void Update(bool floorDetected, bool reticleVisible)
        {
            if (!floorDetected)
                HasFoundFloor = false;
            else if (reticleVisible)
                HasFoundFloor = true;

            var visible = !HasFoundFloor;
            if (visible == IsVisible)
                return;

            IsVisible = visible;
            VisibilityChanged?.Invoke(visible);
        }
    }
}
