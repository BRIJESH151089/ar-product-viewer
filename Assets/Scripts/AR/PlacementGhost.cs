using UnityEngine;
using UnityEngine.Rendering;

namespace ARProductViewer.AR
{
    /// <summary>
    /// Semi-transparent, non-interactive copy of a product used to preview where it will be placed.
    /// </summary>
    public sealed class PlacementGhost : MonoBehaviour
    {
        /// <summary>
        /// Builds a hidden ghost from <paramref name="productPrefab"/>: every renderer uses
        /// <paramref name="ghostMaterial"/>, shadows are off and colliders are removed.
        /// </summary>
        public static PlacementGhost Create(GameObject productPrefab, Material ghostMaterial)
        {
            var instance = Instantiate(productPrefab);
            instance.name = productPrefab.name + " (Ghost)";
            instance.SetActive(false);

            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                Destroy(collider);

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var materials = new Material[renderer.sharedMaterials.Length];
                for (var i = 0; i < materials.Length; i++)
                    materials[i] = ghostMaterial;

                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            return instance.AddComponent<PlacementGhost>();
        }

        public void Show(Pose pose)
        {
            transform.SetPositionAndRotation(pose.position, pose.rotation);
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
