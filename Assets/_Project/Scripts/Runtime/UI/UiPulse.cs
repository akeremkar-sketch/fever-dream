using UnityEngine;

namespace FeverDream.UI
{
    /// <summary>Makes a UI icon beat (scale) and optionally spin. Unscaled time, so it also runs in menus while paused.</summary>
    [DisallowMultipleComponent]
    public sealed class UiPulse : MonoBehaviour
    {
        [Tooltip("How much bigger the icon gets at the top of a beat (0.1 = 10%).")]
        [SerializeField, Min(0f)] private float amount = 0.1f;
        [SerializeField, Min(0.05f)] private float period = 0.9f;
        [SerializeField] private float spinDegreesPerSecond;

        private Vector3 baseScale;
        private Quaternion baseRotation;

        private void Awake()
        {
            baseScale = transform.localScale;
            baseRotation = transform.localRotation;
        }

        private void OnDisable()
        {
            transform.localScale = baseScale;
            transform.localRotation = baseRotation;
        }

        private void Update()
        {
            float time = Time.unscaledTime;
            // A quick swell and a slower fall reads as a heartbeat rather than a sine wobble.
            float beat = Mathf.Pow(Mathf.Abs(Mathf.Sin(time * Mathf.PI / period)), 4f);
            transform.localScale = baseScale * (1f + amount * beat);
            if (spinDegreesPerSecond != 0f)
            {
                transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, -spinDegreesPerSecond * time);
            }
        }
    }
}
