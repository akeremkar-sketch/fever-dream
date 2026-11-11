using UnityEngine;

namespace FeverDream.UI
{
    /// <summary>
    /// One-shot impact for a UI element: Play() snaps it to a start pose (scale and twist) and lets it settle back into
    /// the pose it has in the prefab. With a CanvasGroup the element is hidden until played, holds, then fades out: a
    /// start scale above 1 slams in like a stamp, below 1 bursts outwards. Without one it stays visible and only
    /// punches. Owns the scale and rotation of its object. Unscaled time; waits while its canvas is hidden.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiImpact : MonoBehaviour
    {
        private const float MaxStep = 0.1f;

        [Tooltip("Scale at the moment of impact, relative to the resting scale. 1: no scale change.")]
        [SerializeField, Min(0f)] private float startScale = 1.4f;
        [SerializeField] private float startTwistDegrees;
        [Tooltip("Seconds to settle from the start pose into the resting pose.")]
        [SerializeField, Min(0f)] private float settleTime = 0.2f;
        [Tooltip("Plays weaker than this are skipped, so a loud effect can be kept for big moments.")]
        [SerializeField, Range(0f, 1f)] private float minStrength;

        [Header("Fade (optional)")]
        [Tooltip("Empty: the element stays visible. Set: it is hidden until played, then held and faded out.")]
        [SerializeField] private CanvasGroup fade;
        [SerializeField, Min(0f)] private float holdTime;
        [Tooltip("0 cuts off hard.")]
        [SerializeField, Min(0f)] private float fadeTime;
        [Tooltip("A weaker play is also more transparent.")]
        [SerializeField] private bool dimWithStrength;

        private Canvas canvas;
        private Vector3 baseScale;
        private Quaternion baseRotation;
        private float strength = 1f;
        private float time = -1f;

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            baseScale = transform.localScale;
            baseRotation = transform.localRotation;
            Rest();
        }

        private void OnDisable() => Rest();

        /// <summary>Strength 0-1 scales the start pose: a weak play barely moves.</summary>
        public void Play(float strength = 1f)
        {
            if (!isActiveAndEnabled || strength < minStrength)
            {
                return;
            }
            this.strength = Mathf.Clamp01(strength);
            time = 0f;
            Apply();
        }

        private void Update()
        {
            if (time < 0f || (canvas != null && !canvas.enabled))
            {
                return;
            }
            // Capped, so one long frame (a hitch) does not swallow the impact.
            time += Mathf.Min(Time.unscaledDeltaTime, MaxStep);
            if (time >= settleTime + holdTime + fadeTime)
            {
                Rest();
            }
            else
            {
                Apply();
            }
        }

        private void Apply()
        {
            float kick = settleTime > 0f ? 1f - Mathf.Clamp01(time / settleTime) : 0f;
            kick = kick * kick * strength;
            transform.localScale = baseScale * (1f + (startScale - 1f) * kick);
            transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, startTwistDegrees * kick);
            if (fade == null)
            {
                return;
            }
            // Update only gets here before the end, so fadeTime is above zero whenever the fade has started.
            float fading = time - settleTime - holdTime;
            float alpha = fading <= 0f ? 1f : 1f - fading / fadeTime;
            fade.alpha = dimWithStrength ? alpha * strength : alpha;
        }

        private void Rest()
        {
            time = -1f;
            transform.localScale = baseScale;
            transform.localRotation = baseRotation;
            if (fade != null)
            {
                fade.alpha = 0f;
            }
        }
    }
}
