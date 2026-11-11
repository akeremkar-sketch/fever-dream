using FeverDream.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FeverDream.UI
{
    /// <summary>Opening screen: Play, Controls and Quit. The controls page lists the same controls as the HUD help panel.</summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject controlsPanel;
        [SerializeField] private Text controlsText;
        [SerializeField] private Button playButton;
        [SerializeField] private Button controlsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button controlsBackButton;

        [Tooltip("Scene loaded by Play. It must be in the build profile's scene list.")]
        [SerializeField] private string firstLevelScene = "L_Proto_Dream";

        private void Awake()
        {
            playButton.onClick.AddListener(() => SceneFlow.Load(firstLevelScene));
            controlsButton.onClick.AddListener(() => Show(controlsPanel, controlsBackButton));
            controlsBackButton.onClick.AddListener(() => Show(mainPanel, controlsButton));
            quitButton.onClick.AddListener(SceneFlow.Quit);
            controlsText.text = ControlsText.Lines;
        }

        private void Start()
        {
            // Arriving from a level: time may have been frozen and the cursor locked there.
            GamePause.SetPaused(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Show(mainPanel, playButton);
        }

        private void Show(GameObject page, Button selected)
        {
            mainPanel.SetActive(page == mainPanel);
            controlsPanel.SetActive(page == controlsPanel);
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(selected.gameObject);
            }
        }
    }
}
