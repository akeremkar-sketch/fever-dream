using UnityEngine;

namespace FeverDream.Core
{
    /// <summary>Hit stop and slow motion. All requests are merged so overlapping effects never fight over Time.timeScale.</summary>
    public static class TimeEffects
    {
        private const float HitStopScale = 0.05f;

        private static TimeEffectsRunner runner;
        private static float hitStopUntil;
        private static float slowMotionUntil;
        private static float slowMotionScale = 1f;

        public static bool IsSlowMotion => Time.unscaledTime < slowMotionUntil;

        public static void HitStop(float duration)
        {
            if (duration <= 0f)
            {
                return;
            }
            EnsureRunner();
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

        internal static float EvaluateScale()
        {
            float now = Time.unscaledTime;
            float scale = now < slowMotionUntil ? slowMotionScale : 1f;
            if (now < hitStopUntil)
            {
                scale = Mathf.Min(scale, HitStopScale);
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
            slowMotionUntil = 0f;
            slowMotionScale = 1f;
            Time.timeScale = 1f;
        }
    }
}
