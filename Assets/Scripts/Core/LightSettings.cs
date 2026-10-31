namespace ARProductViewer.Core
{
    /// <summary>
    /// What the scene's main light and ambient lighting should be set to.
    /// </summary>
    public readonly struct LightSettings
    {
        /// <summary>Main light intensity.</summary>
        public readonly float Intensity;

        /// <summary>Main light colour, linear RGB normalised so the brightest channel is 1.</summary>
        public readonly Float3 Color;

        /// <summary>True when <see cref="Direction"/> came from the estimate; otherwise keep the light's current direction.</summary>
        public readonly bool HasDirection;

        /// <summary>Normalised world-space direction the main light shines in.</summary>
        public readonly Float3 Direction;

        /// <summary>Flat ambient colour, linear RGB, for when no ambient spherical harmonics are available.</summary>
        public readonly Float3 AmbientColor;

        public LightSettings(float intensity, Float3 color, bool hasDirection, Float3 direction, Float3 ambientColor)
        {
            Intensity = intensity;
            Color = color;
            HasDirection = hasDirection;
            Direction = direction;
            AmbientColor = ambientColor;
        }
    }
}
