using FeverDream.Core;
using FeverDream.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FeverDream.UI
{
    /// <summary>
    /// In-game menu. Pause (Esc / Start) opens it as the pause menu; a while after the level goal is reached it opens as the
    /// end-of-level menu, without Resume. The control list is a page of this menu: the Controls button shows it and F1
    /// opens the menu straight on it. The game stays paused while the menu is open.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseMenuView : MonoBehaviour, ICancelHandler
    {
        [SerializeField] private GameObject panel;
        [Tooltip("On the panel. Switches the whole menu's input off for a moment when it opens by itself.")]
        [SerializeField] private CanvasGroup panelGroup;
        [Tooltip("The card with the title and the buttons. Hidden while the control list is shown.")]
        [SerializeField] private GameObject mainPage;
        [SerializeField] private ControlsPanel controls;
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button controlsButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;

        [Tooltip("Scene loaded by the Main Menu button. It must be in the build profile's scene list.")]
        [SerializeField] private string mainMenuScene = "MENU_Main";
        [SerializeField] private string pausedTitle = "DURAKLATILDI";
        [SerializeField] private string pausedSubtitle = "RÜYA BEKLİYOR. MESANEN BEKLEMİYOR.";
        [SerializeField] private string levelCompleteTitle = "BÖLÜM TAMAMLANDI";
        [Tooltip("{0} is the player's total Aura. The HUD is hidden behind the menu, so the total is repeated here.")]
        [SerializeField] private string levelCompleteSubtitle = "TOPLAM AURA: {0}";
        [Tooltip("Real-time seconds between reaching the goal and the end-of-level menu, so the alter ego's line can play.")]
        [SerializeField, Min(0f)] private float levelCompleteDelay = 4f;
        [Tooltip("Real-time seconds the end-of-level menu ignores input after it opens by itself. The player may still be "
            + "attacking or jumping at that moment, and the freed cursor appears on top of a button.")]
        [SerializeField, Min(0f)] private float autoOpenInputDelay = 0.5f;

        private bool levelComplete;
        private float levelCompleteMenuTime = -1f;
        private float inputEnableTime = -1f;
        /// <summary>The menu was opened with F1, so leaving the control list goes straight back to the game.</summary>
        private bool openedOnControls;
        /// <summary>Esc is both the game's Pause and the UI's Cancel; only the first of the two acts in a frame.</summary>
        private int lastBackFrame = -1;

        public bool IsOpen => panel.activeSelf;

        private void Awake()
        {
            resumeButton.onClick.AddListener(Resume);
            controlsButton.onClick.AddListener(() => ShowControls(true));
            controls.BackButton.onClick.AddListener(CloseControls);
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
            if (inputEnableTime >= 0f && Time.unscaledTime >= inputEnableTime)
            {
                SetInputEnabled(true);
                SelectDefault();
            }

            if (levelCompleteMenuTime >= 0f && Time.unscaledTime >= levelCompleteMenuTime)
            {
                levelCompleteMenuTime = -1f;
                // The player may have opened the menu already during the delay; then it is left as it is.
                if (!IsOpen)
                {
                    Open();
                    SetInputEnabled(false);
                    Select(null);
                    inputEnableTime = Time.unscaledTime + autoOpenInputDelay;
                }
                return;
            }

            var player = PlayerContext.Current;
            if (player == null)
            {
                return;
            }
            if (player.Input.PausePressed)
            {
                HandleBack(true);
            }
            else if (player.Input.ToggleHelpPressed)
            {
                ToggleControls();
            }
        }

        /// <summary>UI Cancel (Esc, gamepad B), passed up by the selected <see cref="UiMenuButton"/>.</summary>
        public void OnCancel(BaseEventData eventData)
        {
            if (IsOpen)
            {
                HandleBack(false);
            }
        }

        public void Resume()
        {
            if (!levelComplete)
            {
                Close();
            }
        }

        private void HandleBack(bool mayOpen)
        {
            if (lastBackFrame == Time.frameCount)
            {
                return;
            }
            lastBackFrame = Time.frameCount;

            if (!IsOpen)
            {
                if (mayOpen)
                {
                    Open();
                }
            }
            else if (controls.IsShown)
            {
                CloseControls();
            }
            else
            {
                Resume();
            }
        }

        private void ToggleControls()
        {
            if (!IsOpen)
            {
                Open();
                openedOnControls = true;
                ShowControls(true);
            }
            else if (controls.IsShown)
            {
                CloseControls();
            }
            else
            {
                ShowControls(true);
            }
        }

        private void Open()
        {
            // Opened by hand during the end-of-level delay: the timer must not open it a second time.
            levelCompleteMenuTime = -1f;
            titleText.text = levelComplete ? levelCompleteTitle : pausedTitle;
            var player = PlayerContext.Current;
            subtitleText.text = levelComplete && player != null ? string.Format(levelCompleteSubtitle, player.Wallet.Total) : pausedSubtitle;
            resumeButton.gameObject.SetActive(!levelComplete);
            panel.SetActive(true);
            SetInputEnabled(true);
            openedOnControls = false;
            GamePause.SetPaused(true);
            ShowControls(false);
        }

        private void ShowControls(bool show)
        {
            bool wasShown = controls.IsShown;
            mainPage.SetActive(!show);
            controls.Show(show);
            Select(show ? controls.BackButton : wasShown ? controlsButton : DefaultButton);
        }

        private void CloseControls()
        {
            if (openedOnControls && !levelComplete)
            {
                Close();
            }
            else
            {
                openedOnControls = false;
                ShowControls(false);
            }
        }

        private void Close()
        {
            panel.SetActive(false);
            // Back to the first page, so the next Open starts from a known state.
            controls.Show(false);
            mainPage.SetActive(true);
            SetInputEnabled(true);
            openedOnControls = false;
            Select(null);
            GamePause.SetPaused(false);
        }

        private Button DefaultButton => levelComplete ? restartButton : resumeButton;

        private void SelectDefault() => Select(controls.IsShown ? controls.BackButton : DefaultButton);

        /// <summary>A selected button lets a gamepad or the arrow keys drive the menu.</summary>
        private static void Select(Button button)
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(button != null ? button.gameObject : null);
            }
        }

        private void SetInputEnabled(bool on)
        {
            panelGroup.interactable = on;
            panelGroup.blocksRaycasts = on;
            inputEnableTime = -1f;
        }

        private void HandleLevelGoal(LevelGoal goal)
        {
            levelComplete = true;
            levelCompleteMenuTime = Time.unscaledTime + levelCompleteDelay;
        }
    }
}
