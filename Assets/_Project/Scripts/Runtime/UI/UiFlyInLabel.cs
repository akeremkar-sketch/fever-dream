using UnityEngine;
using UnityEngine.UI;

namespace FeverDream.UI
{
    /// <summary>
    /// A short text that pops up where it sits, waits just long enough to be read, then shoots into a target and
    /// vanishes ("+120" flying into a total). Cosmetic only: nothing may depend on it arriving. Owns the position,
    /// scale and rotation of its object. Unscaled time; waits while its canvas is hidden.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Text))]
    public sealed class UiFlyInLabel : MonoBehaviour
    {
        private const float MaxStep = 0.1f;

        [Tooltip("The label flies to the centre of this rect. This object's own pivot must be at its centre.")]
        [SerializeField] private RectTransform target;
        [Tooltip("How much bigger the label is when it appears (0.45 = 45%).")]
        [SerializeField, Min(0f)] private float popScale = 0.45f;
        [SerializeField] private float popTwistDegrees = -8f;
        [SerializeField, Min(0.01f)] private float popDuration = 0.12f;
        [SerializeField, Min(0f)] private float holdDuration = 0.16f;
        [SerializeField, Min(0.01f)] private float flyDuration = 0.2f;
        [Tooltip("Scale on arrival. Thin and tall reads as a streak.")]
        [SerializeField] private Vector2 arrivalScale = new Vector2(0.55f, 1.35f);

        private Text label;
        private RectTransform rect;
        private Canvas canvas;
        private Vector2 restPosition;
        private Vector2 flight;
        private float time = -1f;

        /// <summary>Seconds from Play until the label reaches its target.</summary>
        public float Duration => popDuration + holdDuration + flyDuration;

        private void Awake()
        {
            label = GetComponent<Text>();
            rect = (RectTransform)transform;
            canvas = GetComponentInParent<Canvas>();
            restPosition = rect.anchoredPosition;
            label.text = string.Empty;
            if (target == null)
            {
                Debug.LogError($"{name}: UiFlyInLabel has no target assigned.", this);
                enabled = false;
            }
        }

        private void OnDisable() => Hide();

        public void Play(string text, Color color)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }
            Hide();
            label.text = text;
            label.color = color;
            // Measured on every play, so it is right at any aspect ratio and wherever the target sits at that moment.
            Vector3 end = rect.parent.InverseTransformPoint(target.TransformPoint(target.rect.center));
            flight = end - rect.localPosition;
            time = 0f;
            Apply();
        }

        /// <summary>Clears the label at once, for example when another message takes its spot.</summary>
        public void Stop() => Hide();

        private void Update()
        {
            if (time < 0f || (canvas != null && !canvas.enabled))
            {
                return;
            }
            // Capped, so one long frame (a hitch) does not swallow the flight.
            time += Mathf.Min(Time.unscaledDeltaTime, MaxStep);
            if (time >= Duration)
            {
                Hide();
            }
            else
            {
                Apply();
            }
        }

        private void Apply()
        {
            float flyTime = time - popDuration - holdDuration;
            if (flyTime <= 0f)
            {
                float kick = Mathf.Clamp01(1f - time / popDuration);
                kick *= kick;
                rect.anchoredPosition = restPosition;
                rect.localScale = Vector3.one * (1f + popScale * kick);
                rect.localRotation = Quaternion.Euler(0f, 0f, popTwistDegrees * kick);
                return;
            }

            float u = flyTime / flyDuration;
            // Ease-in-back: dips away from the target first, then accelerates into it.
            float eased = u * u * (2.70158f * u - 1.70158f);
            float squash = u * u;
            rect.anchoredPosition = restPosition + flight * eased;
            rect.localScale = new Vector3(Mathf.Lerp(1f, arrivalScale.x, squash), Mathf.Lerp(1f, arrivalScale.y, squash), 1f);
            rect.localRotation = Quaternion.identity;
        }

        private void Hide()
        {
            time = -1f;
            label.text = string.Empty;
            rect.anchoredPosition = restPosition;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }
    }
}
