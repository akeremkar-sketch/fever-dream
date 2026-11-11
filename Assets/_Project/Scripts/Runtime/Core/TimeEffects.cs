using UnityEngine;

namespace FeverDream.Core
{
    /// <summary>
    /// Hit stop, slow motion and the pause. All requests are merged so overlapping effects never fight over Time.timeScale.
    /// </summary>
    public static class TimeEffects
    {
        private static TimeEffectsRunner runner;
        private static float hitStopUntil;
        private static float hitStopScale = 1f;
        private static float slowMotionUntil;
        private static float slowMotionScale = 1f;

        public static bool IsSlowMotion => Time.unscaledTime < slowMotionUntil;

        /// <param name="duration">Real-time seconds.</param>
        /// <param name="scale">Time scale while the hit stop lasts (close to zero freezes the action).</param>
        public static void HitStop(float duration, float scale)
        {
            if (duration <= 0f)
            {
                return;
            }
            EnsureRunner();
            // While an earlier hit stop is still running, the stronger (smaller) scale wins.
            float clamped = Mathf.Clamp(scale, 0.01f, 1f);
            hitStopScale = Time.unscaledTime < hitStopUntil ? Mathf.Min(hitStopScale, clamped) : clamped;
            hitStopUntil = Mathf.Max(hitStopUntil, Time.unscaledTime + duration);
        }

        public static void SlowMotion(float scale, float duration)
        {
            if (duration <= 0f)
            {
                return;
            }
            EnsureRunner();
            slowMotionScale = Mathf.Clamp(scale, 0.05f, 1f);
            slowMotionUntil = Mathf.Max(slowMotionUntil, Time.unscaledTime + duration);
        }

        /// <summary>Applies the merged time scale right away, e.g. when the game is paused or resumed.</summary>
        internal static void Refresh()
        {
            EnsureRunner();
            Time.timeScale = EvaluateScale();
        }

        internal static float EvaluateScale()
        {
            if (GamePause.IsPaused)
            {
                return 0f;
            }
            float now = Time.unscaledTime;
            float scale = now < slowMotionUntil ? slowMotionScale : 1f;
            if (now < hitStopUntil)
            {
                scale = Mathf.Min(scale, hitStopScale);
            }
            return scale;
        }

        private static void EnsureRunner()
        {
            if (runner != null)
            {
                return;
            }
            var host = new GameObject("TimeEffects");
            Object.DontDestroyOnLoad(host);
            runner = host.AddComponent<TimeEffectsRunner>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            runner = null;
            hitStopUntil = 0f;
            hitStopScale = 1f;
            slowMotionUntil = 0f;
            slowMotionScale = 1f;
            Time.timeScale = 1f;
        }
    }
}
