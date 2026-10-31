namespace ARProductViewer.Core
{
    /// <summary>
    /// Light estimation values reported by the AR platform for one camera frame.
    /// Every value is optional: platforms and estimation modes provide different subsets.
    /// </summary>
    public struct LightEstimateInput
    {
        /// <summary>Average scene brightness, roughly 0 (dark) to 1 (bright). ARCore ambient-intensity mode.</summary>
        public float? AverageBrightness;

        /// <summary>Average colour temperature in Kelvin (e.g. ARKit).</summary>
        public float? ColorTemperatureKelvin;

        /// <summary>Per-channel colour correction (RGB). ARCore ambient-intensity mode.</summary>
        public Float3? ColorCorrection;

        /// <summary>World-space direction the main light shines in. ARCore Environmental HDR.</summary>
        public Float3? MainLightDirection;

        /// <summary>Main light colour including its intensity, linear RGB (may exceed 1). ARCore Environmental HDR.</summary>
        public Float3? MainLightColor;

        /// <summary>Brightness of the main light. ARCore Environmental HDR.</summary>
        public float? MainLightBrightness;
    }
}
