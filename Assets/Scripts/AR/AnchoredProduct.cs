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
    /// While the user manipulates the product, anchor following is suspended; when manipulation ends
    /// the product is re-anchored at its new pose.
    /// </summary>
    public sealed class AnchoredProduct : MonoBehaviour
    {
        static int s_NextId;

        ARAnchor m_Anchor;
        ARAnchorManager m_AnchorManager;
        AnchorFollower m_Follower;
        int m_AnchorRequest;

        /// <summary>Unique handle for this placed product, used by selection.</summary>
        public int Id { get; private set; }

        public ARAnchor Anchor => m_Anchor;

        /// <summary>The pose the product was last placed or dropped at.</summary>
        public PlacementPose IntendedPose => m_Follower.IntendedPose;

        /// <summary>True once the anchor has been seen tracking at the intended pose.</summary>
        public bool IsLockedToAnchor => m_Follower != null && m_Follower.IsLocked;

        public bool IsManipulating { get; private set; }

        /// <summary>The product's current upright pose.</summary>
        public PlacementPose CurrentPose => new PlacementPose(transform.position.ToFloat3(), transform.eulerAngles.y);

        /// <summary>Uniform scale relative to the product's real size.</summary>
        public float Scale
        {
            get => transform.localScale.x;
            set => transform.localScale = Vector3.one * value;
        }

        void Awake()
        {
            Id = ++s_NextId;
        }

        /// <summary>
        /// Puts the product at <paramref name="pose"/> and anchors it there, attached to <paramref name="plane"/>
        /// when possible so the anchor follows plane refinements. Without an anchor the product stays put.
        /// </summary>
        public void Place(PlacementPose pose, ARPlane plane, ARAnchorManager anchorManager)
        {
            m_AnchorManager = anchorManager;
            m_Follower = new AnchorFollower(pose);
            Apply(pose);

            if (plane != null && CanUseAnchorManager())
            {
                var attached = m_AnchorManager.AttachAnchor(plane, pose.ToUnityPose());
                if (attached != null)
                {
                    m_Anchor = attached;
                    return;
                }
            }

            RequestAnchor(pose);
        }

        /// <summary>Stops following the anchor so the product can be moved or rotated directly.</summary>
        public void BeginManipulation()
        {
            IsManipulating = true;
        }

        /// <summary>Sets the product's pose while manipulating.</summary>
        public void SetPose(PlacementPose pose)
        {
            Apply(pose);
        }

        /// <summary>Re-anchors the product at its current pose and resumes anchor following.</summary>
        public void EndManipulation()
        {
            if (!IsManipulating)
                return;

            IsManipulating = false;
            Reanchor(CurrentPose);
        }

        void LateUpdate()
        {
            if (IsManipulating || m_Follower == null || m_Anchor == null)
                return;

            var anchorTransform = m_Anchor.transform;
            var anchorPose = new PlacementPose(anchorTransform.position.ToFloat3(), anchorTransform.eulerAngles.y);
            Apply(m_Follower.Update(m_Anchor.trackingState == TrackingState.Tracking, anchorPose));
        }

        void OnDestroy()
        {
            m_AnchorRequest++;
            ReleaseAnchor();
        }

        void Reanchor(PlacementPose pose)
        {
            ReleaseAnchor();
            m_Follower = new AnchorFollower(pose);
            RequestAnchor(pose);
        }

        async void RequestAnchor(PlacementPose pose)
        {
            if (!CanUseAnchorManager())
                return;

            var request = ++m_AnchorRequest;
            var result = await m_AnchorManager.TryAddAnchorAsync(pose.ToUnityPose());
            if (!result.status.IsSuccess())
            {
                if (this != null)
                    Debug.LogWarning($"{nameof(AnchoredProduct)}: could not create an anchor; product stays where it is.", this);
                return;
            }

            // The product was destroyed or manipulated again while the anchor was being created.
            if (this == null || request != m_AnchorRequest || IsManipulating)
            {
                if (CanUseAnchorManager() && result.value != null)
                    m_AnchorManager.TryRemoveAnchor(result.value);
                return;
            }

            m_Anchor = result.value;
        }

        void ReleaseAnchor()
        {
            if (m_Anchor != null && CanUseAnchorManager())
                m_AnchorManager.TryRemoveAnchor(m_Anchor);
            m_Anchor = null;
        }

        bool CanUseAnchorManager() => m_AnchorManager != null && m_AnchorManager.enabled;

        void Apply(PlacementPose pose)
        {
            var unityPose = pose.ToUnityPose();
            transform.SetPositionAndRotation(unityPose.position, unityPose.rotation);
        }
    }
}
