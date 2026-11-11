using UnityEngine;
using UnityEngine.UI;

namespace FeverDream.UI
{
    /// <summary>
    /// Vertical colour gradient multiplied into a UI graphic. Put it above Outline and Shadow in the component list,
    /// so the outline keeps its own colour.
    /// </summary>
    [AddComponentMenu("UI/Effects/Fever Dream Gradient")]
    public sealed class UiGradient : BaseMeshEffect
    {
        [SerializeField] private Color top = Color.white;
        [SerializeField] private Color bottom = new Color(0.8f, 0.8f, 0.8f, 1f);

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0)
            {
                return;
            }
            var rect = graphic.rectTransform.rect;
            var vertex = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                float t = Mathf.InverseLerp(rect.yMin, rect.yMax, vertex.position.y);
                vertex.color = (Color)vertex.color * Color.Lerp(bottom, top, t);
                vh.SetUIVertex(vertex, i);
            }
        }
    }
}
