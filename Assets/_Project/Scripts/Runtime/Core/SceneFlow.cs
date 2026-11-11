using UnityEngine;
using UnityEngine.SceneManagement;

namespace FeverDream.Core
{
    /// <summary>Scene changes and quitting. Always leaves the game unpaused, so the next scene starts with time running.</summary>
    public static class SceneFlow
    {
        public static void Load(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"SceneFlow: scene '{sceneName}' is not in the build profile's scene list.");
                return;
            }
            GamePause.SetPaused(false);
            SceneManager.LoadScene(sceneName);
        }

        public static void ReloadActive() => Load(SceneManager.GetActiveScene().name);

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
