using UnityEngine;

namespace FeverDream.Core
{
    /// <summary>Created on demand by <see cref="TimeEffects"/>; applies the merged time scale every frame.</summary>
    [AddComponentMenu("")]
    internal sealed class TimeEffectsRunner : MonoBehaviour
    {
        private void Update()
        {
            Time.timeScale = TimeEffects.EvaluateScale();
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}
