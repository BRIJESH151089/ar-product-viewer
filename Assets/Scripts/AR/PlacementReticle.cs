using UnityEngine;

namespace ARProductViewer.AR
{
    /// <summary>
    /// Small marker lying flat on the floor at the exact point a product will be placed.
    /// </summary>
    public sealed class PlacementReticle : MonoBehaviour
    {
        [SerializeField, Tooltip("Lift above the surface, in metres, to avoid z-fighting with the plane visualizer.")]
        float m_SurfaceOffset = 0.003f;

        public void Show(Pose pose)
        {
            transform.SetPositionAndRotation(pose.position + Vector3.up * m_SurfaceOffset, pose.rotation);
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }
    }
}
