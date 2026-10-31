using UnityEngine;

namespace ARProductViewer.AR
{
    /// <summary>
    /// Soft contact shadow under a placed product. It is parented to the product, so it moves, rotates
    /// and scales with it; it only needs sizing once to the product's footprint.
    /// </summary>
    public sealed class BlobShadow : MonoBehaviour
    {
        [SerializeField, Tooltip("Shadow size relative to the product's footprint.")]
        float m_Padding = 1.3f;

        [SerializeField, Tooltip("Lift above the floor, in metres, to avoid z-fighting with the plane visualizer.")]
        float m_SurfaceOffset = 0.002f;

        /// <summary>Creates a shadow under <paramref name="product"/>, sized to its root BoxCollider footprint.</summary>
        public static BlobShadow Attach(BlobShadow prefab, GameObject product)
        {
            var shadow = Instantiate(prefab, product.transform, false);
            shadow.name = prefab.name;
            shadow.FitTo(product.GetComponent<BoxCollider>());
            return shadow;
        }

        void FitTo(BoxCollider footprint)
        {
            var size = footprint != null ? new Vector2(footprint.size.x, footprint.size.z) : Vector2.one * 0.5f;
            var center = footprint != null ? footprint.center : Vector3.zero;
            transform.localPosition = new Vector3(center.x, m_SurfaceOffset, center.z);
            transform.localRotation = Quaternion.identity;
            transform.localScale = new Vector3(size.x * m_Padding, 1f, size.y * m_Padding);
        }
    }
}
