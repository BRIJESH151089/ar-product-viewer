using System;
using ARProductViewer.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ARProductViewer.UI
{
    /// <summary>
    /// One entry in the product picker: thumbnail, name and a selected-state highlight.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class ProductButton : MonoBehaviour
    {
        [SerializeField]
        Image m_Thumbnail;

        [SerializeField]
        TMP_Text m_Label;

        [SerializeField, Tooltip("Shown only while this product is selected.")]
        Graphic m_SelectionHighlight;

        Button m_Button;
        Action m_OnClicked;

        public string ProductId { get; private set; }

        void Awake()
        {
            m_Button = GetComponent<Button>();
            m_Button.onClick.AddListener(HandleClick);
        }

        void OnDestroy()
        {
            m_Button.onClick.RemoveListener(HandleClick);
        }

        public void Bind(ProductInfo product, Sprite thumbnail, Action onClicked)
        {
            ProductId = product.Id;
            m_OnClicked = onClicked;
            name = $"ProductButton ({product.Id})";

            m_Label.text = product.DisplayName;
            m_Thumbnail.sprite = thumbnail;
            m_Thumbnail.enabled = thumbnail != null;
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            m_SelectionHighlight.enabled = selected;
            m_Label.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
        }

        void HandleClick() => m_OnClicked?.Invoke();
    }
}
