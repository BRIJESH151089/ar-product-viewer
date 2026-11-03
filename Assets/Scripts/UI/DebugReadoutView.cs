using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ARProductViewer.UI
{
    /// <summary>
    /// Small on-screen debug panel toggled by a button. Shows whatever text it is given; hidden by default.
    /// </summary>
    public sealed class DebugReadoutView : MonoBehaviour
    {
        [SerializeField]
        Button m_ToggleButton;

        [SerializeField, Tooltip("Tinted while the readout is shown.")]
        Image m_ToggleBackground;

        [SerializeField]
        GameObject m_Panel;

        [SerializeField]
        TMP_Text m_Text;

        [SerializeField]
        Color m_OffColor = new Color(0.08f, 0.09f, 0.11f, 0.6f);

        [SerializeField]
        Color m_OnColor = new Color(0.35f, 0.8f, 1f, 0.9f);

        public bool IsVisible => m_Panel.activeSelf;

        void Awake()
        {
            m_ToggleButton.onClick.AddListener(Toggle);
            SetVisible(false);
        }

        void OnDestroy() => m_ToggleButton.onClick.RemoveListener(Toggle);

        public void SetText(string text)
        {
            if (m_Text.text != text)
                m_Text.text = text;
        }

        public void SetVisible(bool visible)
        {
            m_Panel.SetActive(visible);
            if (m_ToggleBackground != null)
                m_ToggleBackground.color = visible ? m_OnColor : m_OffColor;
        }

        void Toggle() => SetVisible(!IsVisible);
    }
}
