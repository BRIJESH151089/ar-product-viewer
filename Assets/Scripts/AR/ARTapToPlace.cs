using System.Collections.Generic;
using ARProductViewer.Core;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARProductViewer.AR
{
    /// <summary>
    /// Raycasts from the screen centre onto detected planes, previews the product there with a ghost and
    /// reticle, and places an anchored product when asked via <see cref="PlaceAtReticle"/> (taps are handled
    /// by <see cref="FurnitureInteraction"/>). Only floor-level planes are accepted. Placement and floor
    /// decisions are made by <see cref="PlacementService"/> and <see cref="FloorEstimator"/>;
    /// this component only converts AR data to Core data and back.
    /// </summary>
    [RequireComponent(typeof(ARRaycastManager), typeof(ARAnchorManager), typeof(ARPlaneManager))]
    public sealed class ARTapToPlace : MonoBehaviour
    {
        static readonly List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

        readonly List<SurfaceHit> m_SurfaceHits = new List<SurfaceHit>();
        readonly List<ARPlane> m_HitPlanes = new List<ARPlane>();
        readonly List<HorizontalPlane> m_FloorCandidates = new List<HorizontalPlane>();

        [SerializeField, Tooltip("Product placed on tap. Its pivot should be at the bottom centre. Can be changed at runtime with SetProductPrefab.")]
        GameObject m_ProductPrefab;

        [SerializeField, Tooltip("Material applied to every renderer of the placement preview.")]
        Material m_GhostMaterial;

        [SerializeField, Tooltip("Marker shown on the floor at the exact placement point. Optional.")]
        PlacementReticle m_ReticlePrefab;

        [SerializeField, Tooltip("Soft contact shadow added under every placed product. Optional.")]
        BlobShadow m_ShadowPrefab;

        [SerializeField, Tooltip("AR camera. Defaults to Camera.main.")]
        Camera m_Camera;

        readonly PlacementService m_Placement = new PlacementService();

        ARRaycastManager m_RaycastManager;
        ARAnchorManager m_AnchorManager;
        ARPlaneManager m_PlaneManager;
        PlacementGhost m_Ghost;
        PlacementReticle m_Reticle;
        ARPlane m_CandidatePlane;

        /// <summary>The prefab currently previewed and placed.</summary>
        public GameObject ProductPrefab => m_ProductPrefab;

        /// <summary>The floor height estimate from the latest frame.</summary>
        public FloorEstimate Floor { get; private set; }

        /// <summary>True while the reticle is over the floor and a product prefab is set.</summary>
        public bool CanPlace => m_Placement.CanPlace && m_ProductPrefab != null;

        /// <summary>Hides the ghost and reticle, e.g. while the user manipulates a placed product.</summary>
        public bool SuppressPreview { get; set; }

        /// <summary>
        /// Places the current product exactly at the pose the reticle and ghost show this frame.
        /// Returns null when <see cref="CanPlace"/> is false.
        /// </summary>
        public AnchoredProduct PlaceAtReticle()
        {
            if (m_ProductPrefab == null || !m_Placement.TryConfirm(out var pose))
                return null;

            return PlaceProduct(m_ProductPrefab, pose, m_CandidatePlane);
        }

        void Awake()
        {
            m_RaycastManager = GetComponent<ARRaycastManager>();
            m_AnchorManager = GetComponent<ARAnchorManager>();
            m_PlaneManager = GetComponent<ARPlaneManager>();
            if (m_Camera == null)
                m_Camera = Camera.main;

            if (m_ReticlePrefab != null)
            {
                m_Reticle = Instantiate(m_ReticlePrefab);
                m_Reticle.Hide();
            }

            RebuildGhost();
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

        void OnDisable()
        {
            if (m_Ghost != null)
                m_Ghost.Hide();
            if (m_Reticle != null)
                m_Reticle.Hide();
        }

        void OnDestroy()
        {
            if (m_Ghost != null)
                Destroy(m_Ghost.gameObject);
            if (m_Reticle != null)
                Destroy(m_Reticle.gameObject);
        }

        void Update()
        {
            Floor = EstimateFloor();
            ProbeScreenCentre();
            m_Placement.UpdateHits(m_SurfaceHits, m_Camera.transform.position.ToFloat3(), Floor);
            m_CandidatePlane = m_Placement.CanPlace ? m_HitPlanes[m_Placement.SelectedHitIndex] : null;

            UpdatePreview();
        }

        /// <summary>Shows the ghost and reticle at the placement pose while over the floor; hides both otherwise.</summary>
        void UpdatePreview()
        {
            if (m_Placement.CanPlace && !SuppressPreview)
            {
                var pose = m_Placement.CurrentPose.ToUnityPose();
                if (m_Ghost != null)
                    m_Ghost.Show(pose);
                if (m_Reticle != null)
                    m_Reticle.Show(pose);
            }
            else
            {
                if (m_Ghost != null)
                    m_Ghost.Hide();
                if (m_Reticle != null)
                    m_Reticle.Hide();
            }
        }

        FloorEstimate EstimateFloor()
        {
            m_FloorCandidates.Clear();
            foreach (var plane in m_PlaneManager.trackables)
            {
                if (plane.subsumedBy != null
                    || plane.trackingState != TrackingState.Tracking
                    || plane.alignment != PlaneAlignment.HorizontalUp)
                    continue;

                var isClassifiedFloor = (plane.classifications & PlaneClassifications.Floor) != 0;
                m_FloorCandidates.Add(new HorizontalPlane(plane.center.y, isClassifiedFloor));
            }

            return FloorEstimator.Estimate(m_FloorCandidates);
        }

        /// <summary>
        /// Collects every plane hit along the centre ray, closest first, so Core can skip
        /// elevated surfaces (tables, beds) and pick the floor behind them.
        /// </summary>
        void ProbeScreenCentre()
        {
            m_SurfaceHits.Clear();
            m_HitPlanes.Clear();

            var screenCentre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (!m_RaycastManager.Raycast(screenCentre, s_Hits, TrackableType.PlaneWithinPolygon))
                return;

            foreach (var hit in s_Hits)
            {
                var plane = hit.trackable as ARPlane;
                var isHorizontalUp = plane != null && plane.alignment == PlaneAlignment.HorizontalUp;
                m_SurfaceHits.Add(new SurfaceHit(true, isHorizontalUp, hit.pose.position.ToFloat3()));
                m_HitPlanes.Add(plane);
            }
        }

        AnchoredProduct PlaceProduct(GameObject productPrefab, PlacementPose intendedPose, ARPlane plane)
        {
            // Spawn at the intended floor pose immediately. The product is not parented to its anchor:
            // AnchoredProduct only follows the anchor once it is tracking at this pose.
            var pose = intendedPose.ToUnityPose();
            var product = Instantiate(productPrefab, pose.position, pose.rotation);
            if (m_ShadowPrefab != null)
                BlobShadow.Attach(m_ShadowPrefab, product);
            var anchored = product.AddComponent<AnchoredProduct>();
            anchored.Place(intendedPose, plane, m_AnchorManager);
            return anchored;
        }
    }
}
