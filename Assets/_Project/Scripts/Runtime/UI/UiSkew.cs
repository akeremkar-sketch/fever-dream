using UnityEngine;
using UnityEngine.UI;

namespace FeverDream.UI
{
    /// <summary>Slants a UI graphic sideways like a manga action panel. Works on images and text.</summary>
    [AddComponentMenu("UI/Effects/Fever Dream Skew")]
    public sealed class UiSkew : BaseMeshEffect
    {
        [Tooltip("Degrees. Positive leans the top to the right.")]
        [SerializeField, Range(-30f, 30f)] private float angle = 10f;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0)
            {
                return;
            }
            float centerY = graphic.rectTransform.rect.center.y;
            float shear = Mathf.Tan(angle * Mathf.Deg2Rad);
            var vertex = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                vertex.position.x += (vertex.position.y - centerY) * shear;
                vh.SetUIVertex(vertex, i);
            }
        }
    }
}
