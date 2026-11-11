using UnityEngine;

namespace FeverDream.UI
{
    /// <summary>Gently bobs and sways a UI element, so menus feel like a dream instead of a form. Unscaled time.</summary>
    [DisallowMultipleComponent]
    public sealed class UiIdleFloat : MonoBehaviour
    {
        [SerializeField] private float bobPixels = 6f;
        [SerializeField] private float swayDegrees = 1.5f;
        [SerializeField, Min(0.01f)] private float period = 3.2f;

        private RectTransform rect;
        private Vector2 basePosition;
        private Quaternion baseRotation;
        private float phase;

        private void Awake()
        {
            rect = (RectTransform)transform;
            basePosition = rect.anchoredPosition;
            baseRotation = rect.localRotation;
            // Elements next to each other should not move in lockstep.
            phase = (transform.GetSiblingIndex() * 0.37f) % 1f;
        }

        private void Update()
        {
            float a = (Time.unscaledTime / period + phase) * Mathf.PI * 2f;
            rect.anchoredPosition = basePosition + new Vector2(0f, Mathf.Sin(a) * bobPixels);
            rect.localRotation = baseRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(a * 0.5f) * swayDegrees);
        }
    }
}
