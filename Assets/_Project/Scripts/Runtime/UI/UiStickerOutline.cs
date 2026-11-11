using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace FeverDream.UI
{
    /// <summary>
    /// Comic lettering for UI text: an even outline drawn in several directions, plus a hard offset shadow of the outlined
    /// shape. Unity's own Outline only draws four diagonal copies, which turns thick outlines jagged.
    /// Put it below UiGradient and UiSkew in the component list.
    /// </summary>
    [AddComponentMenu("UI/Effects/Fever Dream Sticker Outline")]
    public sealed class UiStickerOutline : BaseMeshEffect
    {
        [SerializeField] private Color outlineColor = new Color(0.07f, 0.03f, 0.13f, 1f);
        [SerializeField, Range(0f, 10f)] private float outlineWidth = 3f;
        [Tooltip("More directions give a rounder outline. 8 is enough up to about 3 px.")]
        [SerializeField, Range(4, 16)] private int directions = 8;
        [Tooltip("Alpha 0 turns the shadow off.")]
        [SerializeField] private Color shadowColor = new Color(0.82f, 0.14f, 0.52f, 1f);
        [SerializeField] private Vector2 shadowOffset = new Vector2(5f, -5f);

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0)
            {
                return;
            }

            var source = ListPool<UIVertex>.Get();
            var output = ListPool<UIVertex>.Get();
            vh.GetUIVertexStream(source);

            bool hasOutline = outlineWidth > 0f && outlineColor.a > 0f;
            bool hasShadow = shadowColor.a > 0f && shadowOffset != Vector2.zero;
            // Drawn back to front: the shadow of the outlined shape, the outline, then the lettering itself.
            if (hasShadow)
            {
                if (hasOutline)
                {
                    AppendRing(source, output, shadowOffset, shadowColor);
                }
                Append(source, output, shadowOffset, shadowColor);
            }
            if (hasOutline)
            {
                AppendRing(source, output, Vector2.zero, outlineColor);
            }
            output.AddRange(source);

            vh.Clear();
            vh.AddUIVertexTriangleStream(output);
            ListPool<UIVertex>.Release(source);
            ListPool<UIVertex>.Release(output);
        }

        private void AppendRing(List<UIVertex> source, List<UIVertex> output, Vector2 center, Color color)
        {
            for (int i = 0; i < directions; i++)
            {
                float angle = Mathf.PI * 2f * i / directions;
                Append(source, output, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * outlineWidth, color);
            }
        }

        private static void Append(List<UIVertex> source, List<UIVertex> output, Vector2 offset, Color color)
        {
            for (int i = 0; i < source.Count; i++)
            {
                var vertex = source[i];
                vertex.position += new Vector3(offset.x, offset.y, 0f);
                // Keep the lettering's own transparency, so fades still work.
                var tinted = (Color32)color;
                tinted.a = (byte)(tinted.a * vertex.color.a / 255);
                vertex.color = tinted;
                output.Add(vertex);
            }
        }
    }
}
