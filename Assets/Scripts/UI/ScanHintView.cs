using System;
using ARProductViewer.Core;
using UnityEngine;

namespace ARProductViewer.UI
{
    /// <summary>
    /// Centred onboarding hint ("Move your phone slowly to scan the floor") that fades in and out
    /// following a <see cref="ScanHintState"/>. Never blocks touches.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ScanHintView : MonoBehaviour
    {
        [SerializeField, Tooltip("Seconds to fade fully in or out.")]
        float m_FadeDuration = 0.3f;

        CanvasGroup m_Group;
        ScanHintState m_State;
        float m_TargetAlpha = 1f;

        public void Initialize(ScanHintState state)
        {
            Unsubscribe();
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            m_State.VisibilityChanged += OnVisibilityChanged;
            m_TargetAlpha = m_State.IsVisible ? 1f : 0f;
        }

        void Awake()
        {
            m_Group = GetComponent<CanvasGroup>();
            m_Group.blocksRaycasts = false;
            m_Group.interactable = false;
        }

        void OnDestroy() => Unsubscribe();

        void Update()
        {
            if (Mathf.Approximately(m_Group.alpha, m_TargetAlpha))
                return;

            var step = m_FadeDuration > 0f ? Time.unscaledDeltaTime / m_FadeDuration : 1f;
            m_Group.alpha = Mathf.MoveTowards(m_Group.alpha, m_TargetAlpha, step);
        }

        void OnVisibilityChanged(bool visible) => m_TargetAlpha = visible ? 1f : 0f;

        void Unsubscribe()
        {
            if (m_State != null)
                m_State.VisibilityChanged -= OnVisibilityChanged;
        }
    }
}
