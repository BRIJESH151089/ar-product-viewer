using ARProductViewer.Core;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARProductViewer.AR
{
    /// <summary>
    /// Keeps a placed product at the pose the user chose and lets it follow its anchor's corrections
    /// via <see cref="AnchorFollower"/>. The product is deliberately not parented to the anchor, so an
    /// anchor that reports a bogus pose before it is tracking cannot drag the product away.
    /// </summary>
    public sealed class AnchoredProduct : MonoBehaviour
    {
        ARAnchor m_Anchor;
        ARAnchorManager m_AnchorManager;
        AnchorFollower m_Follower;

        public ARAnchor Anchor => m_Anchor;

        /// <summary>The pose the product was placed at.</summary>
        public PlacementPose IntendedPose => m_Follower.IntendedPose;

        /// <summary>True once the anchor has been seen tracking at the intended pose.</summary>
        public bool IsLockedToAnchor => m_Follower != null && m_Follower.IsLocked;

        /// <param name="anchor">May be null, in which case the product simply stays at <paramref name="intendedPose"/>.</param>
        public void Initialize(PlacementPose intendedPose, ARAnchor anchor, ARAnchorManager anchorManager)
        {
            m_Follower = new AnchorFollower(intendedPose);
            m_Anchor = anchor;
            m_AnchorManager = anchorManager;
            Apply(m_Follower.Pose);
        }

        void LateUpdate()
        {
            if (m_Follower == null || m_Anchor == null)
                return;

            var anchorTransform = m_Anchor.transform;
            var anchorPose = new PlacementPose(anchorTransform.position.ToFloat3(), anchorTransform.eulerAngles.y);
            Apply(m_Follower.Update(m_Anchor.trackingState == TrackingState.Tracking, anchorPose));
        }

        void OnDestroy()
        {
            if (m_Anchor != null && m_AnchorManager != null && m_AnchorManager.enabled)
                m_AnchorManager.TryRemoveAnchor(m_Anchor);
        }

        void Apply(PlacementPose pose)
        {
            var unityPose = pose.ToUnityPose();
            transform.SetPositionAndRotation(unityPose.position, unityPose.rotation);
        }
    }
}
