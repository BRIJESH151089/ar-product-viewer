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
            var wasLocked = m_Follower.IsLocked;
            Apply(m_Follower.Update(m_Anchor.trackingState == TrackingState.Tracking, anchorPose));
            LogDebug(wasLocked, anchorTransform.position);
        }

        int m_DebugFrames;

        void LogDebug(bool wasLocked, Vector3 anchorPosition)
        {
            const int k_MaxDebugFrames = 600;
            if (m_DebugFrames++ > k_MaxDebugFrames)
                return;

            var intended = m_Follower.IntendedPose.Position.ToVector3();
            if (!wasLocked && m_Follower.IsLocked)
                PlacementDebug.Log($"{name} locked to anchor at {PlacementDebug.Format(anchorPosition)} (|anchor - intended|={(anchorPosition - intended).magnitude:F3}) after {m_DebugFrames} frames");

            if (m_DebugFrames % 120 == 0)
                PlacementDebug.Log(
                    $"{name} frame {m_DebugFrames}: product={PlacementDebug.Format(transform.position)} |product - intended|={(transform.position - intended).magnitude:F3} " +
                    $"anchor={PlacementDebug.Format(anchorPosition)} tracking={m_Anchor.trackingState} locked={m_Follower.IsLocked}");
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
