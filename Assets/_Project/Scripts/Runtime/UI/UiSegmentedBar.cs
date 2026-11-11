using UnityEngine;
using UnityEngine.UI;

namespace FeverDream.UI
{
    /// <summary>
    /// A bar made of separate chips (Filled images, left to right). Chips empty one by one; the bar shakes when the
    /// value drops and blinks in a warning colour while it is low. Unscaled time, so it keeps moving through hit stop.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiSegmentedBar : MonoBehaviour
    {
        [Tooltip("Chips from first to last. Each must be an Image of type Filled.")]
        [SerializeField] private Image[] segments;
        [SerializeField] private Color fullColor = new Color(1f, 0.42f, 0.68f, 1f);
        [SerializeField] private Color lowColor = new Color(1f, 0.25f, 0.2f, 1f);
        [SerializeField, Range(0f, 1f)] private float lowThreshold = 0.3f;
        [Tooltip("What shakes when the value drops. Empty: this object.")]
        [SerializeField] private RectTransform shakeTarget;
        [SerializeField, Min(0f)] private float shakePixels = 9f;
        [SerializeField, Min(0.01f)] private float shakeDuration = 0.25f;

        private float value = 1f;
        private float shakeTime = -1f;
        private Vector2 restPosition;

        private void Awake()
        {
            if (shakeTarget == null)
            {
                shakeTarget = (RectTransform)transform;
            }
            restPosition = shakeTarget.anchoredPosition;
            Apply();
        }

        public void SetValue(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);
            if (Mathf.Approximately(normalized, value))
            {
                return;
            }
            if (normalized < value)
            {
                shakeTime = 0f;
            }
            value = normalized;
            Apply();
        }

        private void Update()
        {
            if (shakeTime >= 0f)
            {
                shakeTime += Time.unscaledDeltaTime;
                float left = 1f - Mathf.Clamp01(shakeTime / shakeDuration);
                shakeTarget.anchoredPosition = restPosition + Random.insideUnitCircle * (shakePixels * left);
                if (left <= 0f)
                {
                    shakeTime = -1f;
                }
            }

            if (value > 0f && value <= lowThreshold)
            {
                float blink = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 9f);
                SetColor(Color.Lerp(fullColor, lowColor, blink));
            }
        }

        private void Apply()
        {
            for (int i = 0; i < segments.Length; i++)
            {
                segments[i].fillAmount = Mathf.Clamp01(value * segments.Length - i);
            }
            if (value > lowThreshold)
            {
                SetColor(fullColor);
            }
        }

        private void SetColor(Color color)
        {
            for (int i = 0; i < segments.Length; i++)
            {
                segments[i].color = color;
            }
        }
    }
}
