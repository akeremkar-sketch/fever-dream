using FeverDream.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FeverDream.UI
{
    /// <summary>Opening screen: Play, Controls and Quit. The control list is the same PF_ControlsPanel the pause menu shows.</summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuView : MonoBehaviour, ICancelHandler
    {
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private ControlsPanel controls;
        [SerializeField] private Button playButton;
        [SerializeField] private Button controlsButton;
        [SerializeField] private Button quitButton;

        [Tooltip("Scene loaded by Play. It must be in the build profile's scene list.")]
        [SerializeField] private string firstLevelScene = "L_Proto_Dream";

        private void Awake()
        {
            playButton.onClick.AddListener(() => SceneFlow.Load(firstLevelScene));
            controlsButton.onClick.AddListener(() => ShowControls(true));
            controls.BackButton.onClick.AddListener(() => ShowControls(false));
            quitButton.onClick.AddListener(SceneFlow.Quit);
        }

        private void Start()
        {
            // Arriving from a level: time may have been frozen and the cursor locked there.
            GamePause.SetPaused(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ShowControls(false);
        }

        /// <summary>UI Cancel (Esc, gamepad B) leaves the control list.</summary>
        public void OnCancel(BaseEventData eventData)
        {
            if (controls.IsShown)
            {
                ShowControls(false);
            }
        }

        private void ShowControls(bool show)
        {
            bool wasShown = controls.IsShown;
            mainPanel.SetActive(!show);
            controls.Show(show);
            Button first = show ? controls.BackButton : wasShown ? controlsButton : playButton;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(first.gameObject);
            }
        }
    }
}
