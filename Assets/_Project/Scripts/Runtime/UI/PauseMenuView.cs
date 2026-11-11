using FeverDream.Core;
using FeverDream.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FeverDream.UI
{
    /// <summary>
    /// In-game menu. Pause (Esc / Start) opens it as the pause menu; a while after the level goal is reached it opens as the
    /// end-of-level menu, without Resume. The game stays paused while it is open.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseMenuView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text titleText;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;

        [Tooltip("Scene loaded by the Main Menu button. It must be in the build profile's scene list.")]
        [SerializeField] private string mainMenuScene = "MENU_Main";
        [SerializeField] private string pausedTitle = "DURAKLATILDI";
        [SerializeField] private string levelCompleteTitle = "BÖLÜM TAMAMLANDI";
        [Tooltip("Real-time seconds between reaching the goal and the end-of-level menu, so the alter ego's line can play.")]
        [SerializeField, Min(0f)] private float levelCompleteDelay = 4f;

        private bool levelComplete;
        private float levelCompleteMenuTime = -1f;

        public bool IsOpen => panel.activeSelf;

        private void Awake()
        {
            resumeButton.onClick.AddListener(Resume);
            restartButton.onClick.AddListener(SceneFlow.ReloadActive);
            mainMenuButton.onClick.AddListener(() => SceneFlow.Load(mainMenuScene));
            quitButton.onClick.AddListener(SceneFlow.Quit);
            panel.SetActive(false);
        }

        private void OnEnable()
        {
            LevelGoal.Reached += HandleLevelGoal;
        }

        private void OnDisable()
        {
            LevelGoal.Reached -= HandleLevelGoal;
            // Never leave the next scene (or a disabled menu) with the game frozen.
            if (IsOpen)
            {
                Close();
            }
        }

        private void Update()
        {
            if (levelCompleteMenuTime >= 0f && Time.unscaledTime >= levelCompleteMenuTime)
            {
                levelCompleteMenuTime = -1f;
                Open();
                return;
            }

            var player = PlayerContext.Current;
            if (player == null || !player.Input.PausePressed)
            {
                return;
            }
            if (!IsOpen)
            {
                Open();
            }
            else if (!levelComplete)
            {
                Resume();
            }
        }

        public void Resume()
        {
            if (!levelComplete)
            {
                Close();
            }
        }

        private void Open()
        {
            titleText.text = levelComplete ? levelCompleteTitle : pausedTitle;
            resumeButton.gameObject.SetActive(!levelComplete);
            panel.SetActive(true);
            GamePause.SetPaused(true);
            // A selected button lets a gamepad or the arrow keys drive the menu.
            if (EventSystem.current != null)
            {
                var first = levelComplete ? restartButton : resumeButton;
                EventSystem.current.SetSelectedGameObject(first.gameObject);
            }
        }

        private void Close()
        {
            panel.SetActive(false);
            GamePause.SetPaused(false);
        }

        private void HandleLevelGoal(LevelGoal goal)
        {
            levelComplete = true;
            levelCompleteMenuTime = Time.unscaledTime + levelCompleteDelay;
        }
    }
}
