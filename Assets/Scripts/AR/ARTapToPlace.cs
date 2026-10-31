using System.Collections.Generic;
using ARProductViewer.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARProductViewer.AR
{
    /// <summary>
    /// Raycasts from the screen centre onto detected planes, previews the product there with a ghost,
    /// and places an anchored product on tap. Placement decisions are made by <see cref="PlacementService"/>;
    /// this component only converts AR data to Core data and back.
    /// </summary>
    [RequireComponent(typeof(ARRaycastManager), typeof(ARAnchorManager))]
    public sealed class ARTapToPlace : MonoBehaviour
    {
        static readonly List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

        [SerializeField, Tooltip("Product placed on tap. Its pivot should be at the bottom centre.")]
        GameObject m_ProductPrefab;

        [SerializeField, Tooltip("Material applied to every renderer of the placement preview.")]
        Material m_GhostMaterial;

        [SerializeField, Tooltip("AR camera. Defaults to Camera.main.")]
        Camera m_Camera;

        readonly PlacementService m_Placement = new PlacementService();

        ARRaycastManager m_RaycastManager;
        ARAnchorManager m_AnchorManager;
        PlacementGhost m_Ghost;
        InputAction m_PlaceAction;
        ARPlane m_CandidatePlane;
        bool m_IsPlacing;

        void Awake()
        {
            m_RaycastManager = GetComponent<ARRaycastManager>();
            m_AnchorManager = GetComponent<ARAnchorManager>();
            if (m_Camera == null)
                m_Camera = Camera.main;

            m_Ghost = PlacementGhost.Create(m_ProductPrefab, m_GhostMaterial);

            m_PlaceAction = new InputAction("Place", InputActionType.Button);
            m_PlaceAction.AddBinding("<Touchscreen>/primaryTouch/tap");
            m_PlaceAction.AddBinding("<Mouse>/leftButton");
        }

        void OnEnable()
        {
            m_PlaceAction.performed += OnPlacePerformed;
            m_PlaceAction.Enable();
        }

        void OnDisable()
        {
            m_PlaceAction.Disable();
            m_PlaceAction.performed -= OnPlacePerformed;
            m_Ghost.Hide();
        }

        void OnDestroy()
        {
            m_PlaceAction.Dispose();
            if (m_Ghost != null)
                Destroy(m_Ghost.gameObject);
        }

        void Update()
        {
            var hit = ProbeScreenCentre(out m_CandidatePlane);
            m_Placement.UpdateHit(hit, m_Camera.transform.position.ToFloat3());

            if (m_Placement.CanPlace)
                m_Ghost.Show(m_Placement.CurrentPose.ToUnityPose());
            else
                m_Ghost.Hide();
        }

        SurfaceHit ProbeScreenCentre(out ARPlane plane)
        {
            plane = null;
            var screenCentre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (!m_RaycastManager.Raycast(screenCentre, s_Hits, TrackableType.PlaneWithinPolygon))
                return SurfaceHit.None;

            // Hits are sorted by distance; the closest one is what the user is looking at.
            var hit = s_Hits[0];
            plane = hit.trackable as ARPlane;
            var isHorizontalUp = plane != null && plane.alignment == PlaneAlignment.HorizontalUp;
            return new SurfaceHit(true, isHorizontalUp, hit.pose.position.ToFloat3());
        }

        void OnPlacePerformed(InputAction.CallbackContext context)
        {
            if (m_IsPlacing || !m_Placement.TryConfirm(out var pose))
                return;

            PlaceProduct(pose.ToUnityPose(), m_CandidatePlane);
        }

        async void PlaceProduct(Pose pose, ARPlane plane)
        {
            m_IsPlacing = true;
            try
            {
                var anchor = await CreateAnchorAsync(pose, plane);
                var product = Instantiate(m_ProductPrefab, pose.position, pose.rotation);
                if (anchor != null)
                    product.transform.SetParent(anchor.transform, true);
                else
                    Debug.LogWarning($"{nameof(ARTapToPlace)}: could not create an anchor; product placed without one.", product);
            }
            finally
            {
                m_IsPlacing = false;
            }
        }

        async Awaitable<ARAnchor> CreateAnchorAsync(Pose pose, ARPlane plane)
        {
            // Prefer attaching to the plane so the anchor follows plane refinements.
            if (plane != null)
            {
                var attached = m_AnchorManager.AttachAnchor(plane, pose);
                if (attached != null)
                    return attached;
            }

            var result = await m_AnchorManager.TryAddAnchorAsync(pose);
            return result.status.IsSuccess() ? result.value : null;
        }
    }
}
