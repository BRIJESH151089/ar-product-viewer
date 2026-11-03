using ARProductViewer.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;

namespace ARProductViewer.AR
{
    /// <summary>
    /// Matches the scene's main light (and ambient light) to the real room using AR light estimation.
    /// Reads <see cref="ARLightEstimationData"/> from each camera frame, maps it with
    /// <see cref="LightEstimationMapper"/> and smooths the result with <see cref="LightSmoothing"/>.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public sealed class ARLightEstimator : MonoBehaviour
    {
        [SerializeField]
        ARCameraManager m_CameraManager;

        [SerializeField, Tooltip("On ARCore, the HDR flags select Environmental HDR (main light direction/colour and ambient spherical harmonics); AmbientIntensity | AmbientColor selects the simpler ambient mode.")]
        LightEstimation m_RequestedEstimation =
            LightEstimation.MainLightDirection | LightEstimation.MainLightIntensity | LightEstimation.AmbientSphericalHarmonics;

        [SerializeField, Tooltip("Seconds for the light to move halfway to a new estimate. Higher is steadier, lower reacts faster.")]
        float m_SmoothingHalfLife = 0.25f;

        [SerializeField, Tooltip("Camera frames without any estimate before switching from HDR to the simpler ambient mode (some devices do not support Environmental HDR).")]
        int m_FramesBeforeAmbientFallback = 90;

        const LightEstimation k_AmbientMode = LightEstimation.AmbientIntensity | LightEstimation.AmbientColor;

        Light m_Light;
        LightSettings m_Current;
        LightSettings m_Target;
        SphericalHarmonicsL2? m_AmbientProbe;
        int m_FramesWithoutEstimate;
        bool m_TriedAmbientMode;

        /// <summary>Latest values reported by the device (all empty until an estimate arrives).</summary>
        public LightEstimateInput LatestInput { get; private set; }

        /// <summary>What the scene light is currently set to.</summary>
        public LightSettings CurrentSettings => m_Current;

        /// <summary>True until the first estimate arrives; natural fallback lighting is used meanwhile.</summary>
        public bool IsUsingFallback { get; private set; } = true;

        /// <summary>The estimation mode the platform is actually running, e.g. for a debug readout.</summary>
        public string ActiveMode => m_CameraManager != null ? m_CameraManager.currentLightEstimation.ToString() : "None";

        void Awake()
        {
            m_Light = GetComponent<Light>();
            // Start from natural indoor lighting rather than whatever the scene light was authored with.
            m_Current = m_Target = LightEstimationMapper.Fallback;
            Apply();
        }

        void OnEnable()
        {
            if (m_CameraManager == null)
                return;

            m_CameraManager.requestedLightEstimation = m_RequestedEstimation;
            m_CameraManager.frameReceived += OnFrameReceived;
        }

        void OnDisable()
        {
            if (m_CameraManager != null)
                m_CameraManager.frameReceived -= OnFrameReceived;
        }

        void OnFrameReceived(ARCameraFrameEventArgs args)
        {
            var estimate = args.lightEstimation;
            var input = new LightEstimateInput
            {
                AverageBrightness = estimate.averageBrightness,
                ColorTemperatureKelvin = estimate.averageColorTemperature,
                ColorCorrection = ToFloat3(estimate.colorCorrection),
                MainLightDirection = estimate.mainLightDirection?.ToFloat3(),
                MainLightColor = ToFloat3(estimate.mainLightColor),
                MainLightBrightness = estimate.averageMainLightBrightness,
            };

            if (!HasAnyValue(input) && !estimate.ambientSphericalHarmonics.HasValue)
            {
                TryAmbientModeFallback();
                return;
            }

            m_FramesWithoutEstimate = 0;
            LatestInput = input;
            m_Target = LightEstimationMapper.Map(input);
            m_AmbientProbe = estimate.ambientSphericalHarmonics;
            IsUsingFallback = false;
        }

        /// <summary>
        /// If HDR estimation was requested but the device never provides it, ask once for ambient mode instead.
        /// </summary>
        void TryAmbientModeFallback()
        {
            if (m_TriedAmbientMode || !IsUsingFallback || m_RequestedEstimation == k_AmbientMode)
                return;
            if (++m_FramesWithoutEstimate < m_FramesBeforeAmbientFallback)
                return;
            if (m_CameraManager.currentLightEstimation != LightEstimation.None)
                return;

            m_TriedAmbientMode = true;
            m_CameraManager.requestedLightEstimation = k_AmbientMode;
            Debug.Log($"{nameof(ARLightEstimator)}: no light estimate from {m_RequestedEstimation}; switching to {k_AmbientMode}.", this);
        }

        void Update()
        {
            m_Current = LightSmoothing.Smooth(m_Current, m_Target, Time.deltaTime, m_SmoothingHalfLife);
            Apply();
        }

        void Apply()
        {
            m_Light.intensity = m_Current.Intensity;
            m_Light.color = new Color(m_Current.Color.X, m_Current.Color.Y, m_Current.Color.Z);
            if (m_Current.HasDirection)
            {
                var direction = m_Current.Direction.ToVector3();
                if (direction.sqrMagnitude > 0.0001f)
                    m_Light.transform.rotation = Quaternion.LookRotation(direction);
            }

            if (m_AmbientProbe.HasValue)
            {
                RenderSettings.ambientMode = AmbientMode.Skybox;
                RenderSettings.ambientProbe = m_AmbientProbe.Value;
            }
            else
            {
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(m_Current.AmbientColor.X, m_Current.AmbientColor.Y, m_Current.AmbientColor.Z);
            }
        }

        static bool HasAnyValue(LightEstimateInput input) =>
            input.AverageBrightness.HasValue || input.ColorTemperatureKelvin.HasValue || input.ColorCorrection.HasValue
            || input.MainLightDirection.HasValue || input.MainLightColor.HasValue || input.MainLightBrightness.HasValue;

        static Float3? ToFloat3(Color? color) => color.HasValue ? new Float3(color.Value.r, color.Value.g, color.Value.b) : (Float3?)null;
    }
}
