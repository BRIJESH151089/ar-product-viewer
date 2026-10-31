using System.Collections.Generic;
using ARProductViewer.Core;
using UnityEngine;
using UnityEngine.EventSystems;
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
        static readonly List<RaycastResult> s_UIHits = new List<RaycastResult>();

        [SerializeField, Tooltip("Product placed on tap. Its pivot should be at the bottom centre. Can be changed at runtime with SetProductPrefab.")]
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
        bool m_HasPendingTap;
        Vector2 m_PendingTapPosition;

        /// <summary>The prefab currently previewed and placed.</summary>
        public GameObject ProductPrefab => m_ProductPrefab;

        void Awake()
        {
            m_RaycastManager = GetComponent<ARRaycastManager>();
            m_AnchorManager = GetComponent<ARAnchorManager>();
            if (m_Camera == null)
                m_Camera = Camera.main;

            RebuildGhost();

            m_PlaceAction = new InputAction("Place", InputActionType.Button);
            m_PlaceAction.AddBinding("<Touchscreen>/primaryTouch/tap");
            m_PlaceAction.AddBinding("<Mouse>/leftButton");
        }

        /// <summary>
        /// Switches the product that is previewed and placed. Already placed products are unaffected.
        /// </summary>
        public void SetProductPrefab(GameObject productPrefab)
        {
            if (productPrefab == m_ProductPrefab)
                return;

            m_ProductPrefab = productPrefab;
            RebuildGhost();
        }

        void RebuildGhost()
        {
            if (m_Ghost != null)
                Destroy(m_Ghost.gameObject);

            m_Ghost = m_ProductPrefab != null ? PlacementGhost.Create(m_ProductPrefab, m_GhostMaterial) : null;
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
            m_HasPendingTap = false;
            if (m_Ghost != null)
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

            if (m_Ghost != null)
            {
                if (m_Placement.CanPlace)
                    m_Ghost.Show(m_Placement.CurrentPose.ToUnityPose());
                else
                    m_Ghost.Hide();
            }

            if (m_HasPendingTap)
            {
                m_HasPendingTap = false;
                TryPlaceFromTap(m_PendingTapPosition);
            }
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
            // UI state is only reliable outside input event processing, so handle the tap in Update.
            var pointer = Pointer.current;
            if (pointer == null)
                return;

            m_PendingTapPosition = pointer.position.ReadValue();
            m_HasPendingTap = true;
        }

        void TryPlaceFromTap(Vector2 screenPosition)
        {
            if (m_IsPlacing || m_ProductPrefab == null || IsOverUI(screenPosition))
                return;

            if (m_Placement.TryConfirm(out var pose))
                PlaceProduct(m_ProductPrefab, pose.ToUnityPose(), m_CandidatePlane);
        }

        static bool IsOverUI(Vector2 screenPosition)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            var pointerData = new PointerEventData(eventSystem) { position = screenPosition };
            eventSystem.RaycastAll(pointerData, s_UIHits);
            var isOverUI = s_UIHits.Count > 0;
            s_UIHits.Clear();
            return isOverUI;
        }

        async void PlaceProduct(GameObject productPrefab, Pose pose, ARPlane plane)
        {
            m_IsPlacing = true;
            try
            {
                var anchor = await CreateAnchorAsync(pose, plane);
                var product = Instantiate(productPrefab, pose.position, pose.rotation);
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
