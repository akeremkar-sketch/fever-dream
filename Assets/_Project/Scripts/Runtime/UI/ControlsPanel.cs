using UnityEngine;
using UnityEngine.UI;

namespace FeverDream.UI
{
    /// <summary>
    /// The control list card (PF_ControlsPanel). The main menu and the pause menu both show this one prefab, so the list is
    /// edited in a single place. Keep its rows in step with IA_Player.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ControlsPanel : MonoBehaviour
    {
        [SerializeField] private Button backButton;

        public Button BackButton => backButton;
        public bool IsShown => gameObject.activeSelf;

        public void Show(bool visible) => gameObject.SetActive(visible);
    }
}
