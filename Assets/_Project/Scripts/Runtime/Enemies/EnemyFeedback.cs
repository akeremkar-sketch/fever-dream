using UnityEngine;

namespace FeverDream.Enemies
{
    /// <summary>Placeholder feedback without art: hit and block flashes, a pulsing attack telegraph and a death shrink.</summary>
    [DisallowMultipleComponent]
    public sealed class EnemyFeedback : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Renderer[] renderers;
        [Tooltip("Shakes while the attack is telegraphed (the ringing alarm clock).")]
        [SerializeField] private Transform telegraphPart;

        [SerializeField] private Color hitColor = Color.white;
        [SerializeField] private Color blockColor = new Color(0.4f, 0.8f, 1f);
        [SerializeField] private Color telegraphColor = new Color(1f, 0.25f, 0.1f);
        [SerializeField, Min(0f)] private float flashDuration = 0.1f;
        [SerializeField, Min(0f)] private float telegraphShake = 0.04f;
        [SerializeField, Min(0.01f)] private float deathDuration = 0.45f;

        private Color[] baseColors;
        private MaterialPropertyBlock block;
        private Color flashColor;
        private float flashUntil;
        private bool telegraph;
        private float deathTime = -1f;
        private Vector3 baseScale;
        private Vector3 telegraphRestPosition;

        private void Awake()
        {
            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>();
            }
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                var material = renderers[i].sharedMaterial;
                baseColors[i] = material != null && material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId) : Color.white;
            }
            block = new MaterialPropertyBlock();
            baseScale = transform.localScale;
            if (telegraphPart != null)
            {
                telegraphRestPosition = telegraphPart.localPosition;
            }
        }

        public void PlayHit() => Flash(hitColor);

        public void PlayBlock() => Flash(blockColor);

        public void SetTelegraph(bool active) => telegraph = active;

        public void PlayDeath() => deathTime = 0f;

        private void Flash(Color color)
        {
            flashColor = color;
            flashUntil = Time.time + flashDuration;
        }

        private void Update()
        {
            if (deathTime >= 0f)
            {
                deathTime += Time.deltaTime;
                transform.localScale = baseScale * (1f - Mathf.Clamp01(deathTime / deathDuration));
            }

            float pulse = telegraph ? 0.5f + 0.5f * Mathf.Sin(Time.time * 30f) : 0f;
            bool flashing = Time.time < flashUntil;
            for (int i = 0; i < renderers.Length; i++)
            {
                Color color = baseColors[i];
                if (telegraph)
                {
                    color = Color.Lerp(color, telegraphColor, 0.4f + 0.6f * pulse);
                }
                if (flashing)
                {
                    color = flashColor;
                }
                block.SetColor(BaseColorId, color);
                renderers[i].SetPropertyBlock(block);
            }

            if (telegraphPart != null)
            {
                telegraphPart.localPosition = telegraphRestPosition + (telegraph ? Random.insideUnitSphere * telegraphShake : Vector3.zero);
            }
        }
    }
}
