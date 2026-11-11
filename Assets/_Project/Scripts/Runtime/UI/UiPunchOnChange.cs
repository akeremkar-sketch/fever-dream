using UnityEngine;
using UnityEngine.UI;

namespace FeverDream.UI
{
    /// <summary>
    /// Pops a UI element (scale and a small twist) whenever the watched text changes to something non-empty, so combo hits
    /// and Aura gains land with a manga "smack". Runs on unscaled time, so hit stop does not freeze it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiPunchOnChange : MonoBehaviour
    {
        [SerializeField] private Text watched;
        [Tooltip("What pops. Empty: this object.")]
        [SerializeField] private RectTransform target;
        [SerializeField, Min(0f)] private float scalePunch = 0.3f;
        [SerializeField] private float twistDegrees = 6f;
        [SerializeField, Min(0.01f)] private float duration = 0.2f;

        private string lastText;
        private float punchTime = -1f;
        private Vector3 baseScale;
        private Quaternion baseRotation;

        private void Awake()
        {
            if (target == null)
            {
                target = (RectTransform)transform;
            }
            baseScale = target.localScale;
            baseRotation = target.localRotation;
            lastText = watched != null ? watched.text : null;
        }

        private void LateUpdate()
        {
            if (watched == null)
            {
                return;
            }
            if (watched.text != lastText)
            {
                lastText = watched.text;
                if (!string.IsNullOrEmpty(lastText))
                {
                    punchTime = 0f;
                }
            }
            if (punchTime < 0f)
            {
                return;
            }

            punchTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(punchTime / duration);
            float kick = 1f - t;
            kick *= kick;
            target.localScale = baseScale * (1f + scalePunch * kick);
            target.localRotation = baseRotation * Quaternion.Euler(0f, 0f, twistDegrees * kick);
            if (t >= 1f)
            {
                punchTime = -1f;
            }
        }
    }
}
