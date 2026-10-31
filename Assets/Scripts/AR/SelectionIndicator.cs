using UnityEngine;

namespace ARProductViewer.AR
{
    /// <summary>
    /// Ring on the floor under the selected product, sized to its footprint and following its
    /// position, yaw and scale. Hidden when nothing is selected.
    /// </summary>
    [DefaultExecutionOrder(100)] // After AnchoredProduct has moved the target this frame.
    public sealed class SelectionIndicator : MonoBehaviour
    {
        [SerializeField, Tooltip("Ring size relative to the product's footprint.")]
        float m_Padding = 1.25f;

        [SerializeField, Tooltip("Smallest ring size in metres, so small products still get a visible ring.")]
        float m_MinSize = 0.3f;

        [SerializeField, Tooltip("Lift above the floor, in metres, to avoid z-fighting with the plane visualizer.")]
        float m_SurfaceOffset = 0.004f;

        Transform m_Target;
        BoxCollider m_TargetBounds;

        /// <summary>Starts following <paramref name="target"/>, or hides the ring when null.</summary>
        public void Follow(Transform target)
        {
            m_Target = target;
            m_TargetBounds = target != null ? target.GetComponent<BoxCollider>() : null;
            gameObject.SetActive(target != null);
            if (target != null)
                Snap();
        }

        void LateUpdate()
        {
            if (m_Target == null)
            {
                if (gameObject.activeSelf)
                    gameObject.SetActive(false);
                return;
            }

            Snap();
        }

        void Snap()
        {
            var footprint = m_TargetBounds != null
                ? new Vector2(m_TargetBounds.size.x, m_TargetBounds.size.z)
                : Vector2.one * 0.5f;
            var scale = m_Target.lossyScale.x;
            var width = Mathf.Max(m_MinSize, footprint.x * scale * m_Padding);
            var depth = Mathf.Max(m_MinSize, footprint.y * scale * m_Padding);

            transform.SetPositionAndRotation(
                m_Target.position + Vector3.up * m_SurfaceOffset,
                Quaternion.Euler(0f, m_Target.eulerAngles.y, 0f));
            transform.localScale = new Vector3(width, 1f, depth);
        }
    }
}
