using UnityEngine;
namespace IronSand.Art
{
    // These instances belong to a visual, not to an authored Unity asset.
    public sealed class GladiatorMeshOwner : MonoBehaviour
    {
        public Mesh Mesh;
        public Material[] Materials;
        private void OnDestroy()
        {
            ImportedGladiatorVisual.Dispose(Mesh);
            if (Materials != null)
                foreach (Material material in Materials) ImportedGladiatorVisual.Dispose(material);
        }
    }
}
