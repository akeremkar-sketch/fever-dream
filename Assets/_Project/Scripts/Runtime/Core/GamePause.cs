using System;
using UnityEngine;

namespace FeverDream.Core
{
    /// <summary>
    /// Pauses the game: time stops (through <see cref="TimeEffects"/>), gameplay input is ignored and the camera frees the cursor.
    /// </summary>
    public static class GamePause
    {
        public static bool IsPaused { get; private set; }

        /// <summary>Frame on which the game was last resumed. The press that resumed it (a click, gamepad A) must not also act in the game.</summary>
        public static int ResumedFrame { get; private set; } = -1;

        public static event Action<bool> Changed;

        public static void SetPaused(bool paused)
        {
            if (paused == IsPaused)
            {
                return;
            }
            IsPaused = paused;
            if (!paused)
            {
                ResumedFrame = Time.frameCount;
            }
            TimeEffects.Refresh();
            Changed?.Invoke(paused);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsPaused = false;
            ResumedFrame = -1;
            Changed = null;
        }
    }
}
