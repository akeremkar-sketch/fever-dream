using FeverDream.AlterEgo;
using FeverDream.Aura;
using FeverDream.Core;
using FeverDream.Player;
using UnityEngine;
using UnityEngine.UI;

namespace FeverDream.UI
{
    /// <summary>Prototype HUD. Reads the player through <see cref="PlayerContext.Current"/> and only displays state.</summary>
    [DisallowMultipleComponent]
    public sealed class HudView : MonoBehaviour
    {
        [Tooltip("The scene's alter ego. Optional: when empty the HUD uses AlterEgoCommentator.Current (any loaded scene).")]
        [SerializeField] private AlterEgoCommentator commentator;

        [Header("Health")]
        [SerializeField] private RectTransform healthFill;

        [Header("Combo")]
        [SerializeField] private CanvasGroup comboGroup;
        [SerializeField] private Text comboHitsText;
        [SerializeField] private Text comboPointsText;
        [SerializeField] private RectTransform comboTimerFill;

        [Header("Aura")]
        [SerializeField] private Text auraTotalText;
        [SerializeField] private Text auraChangeText;
        [SerializeField] private Color auraGainColor = new Color(0.45f, 0.95f, 1f);
        [SerializeField] private Color auraLossColor = new Color(1f, 0.35f, 0.4f);

        [Header("Alter Ego")]
        [SerializeField] private CanvasGroup alterEgoGroup;
        [SerializeField] private Text alterEgoNameText;
        [SerializeField] private Text alterEgoLineText;
        [SerializeField] private Color calmColor = Color.white;
        [SerializeField] private Color uneasyColor = new Color(1f, 0.85f, 0.45f);
        [SerializeField] private Color panicColor = new Color(1f, 0.45f, 0.45f);

        [Header("Aim")]
        [SerializeField] private RectTransform grappleMarker;

        [Header("Messages")]
        [SerializeField] private Text centerMessageText;
        [SerializeField] private GameObject helpPanel;
        [Tooltip("Filled from ControlsText, so the HUD and the main menu always list the same controls.")]
        [SerializeField] private Text helpText;

        private PlayerContext player;
        private AlterEgoCommentator boundCommentator;
        private float alterEgoHideTime;
        private float auraChangeHideTime;
        private float centerMessageHideTime;
        private bool levelComplete;

        private void Awake()
        {
            if (helpText != null)
            {
                helpText.text = ControlsText.HudHelp;
            }
        }

        private void OnEnable()
        {
            PlayerContext.Registered += Bind;
            LevelGoal.Reached += HandleLevelGoal;
            if (PlayerContext.Current != null)
            {
                Bind(PlayerContext.Current);
            }

            AlterEgoCommentator.Registered += BindCommentator;
            BindCommentator(commentator != null ? commentator : AlterEgoCommentator.Current);
        }

        private void OnDisable()
        {
            PlayerContext.Registered -= Bind;
            LevelGoal.Reached -= HandleLevelGoal;
            AlterEgoCommentator.Registered -= BindCommentator;
            Unbind();
            UnbindCommentator();
        }

        private void Start()
        {
            comboGroup.alpha = 0f;
            alterEgoGroup.alpha = 0f;
            auraChangeText.text = string.Empty;
            centerMessageText.text = string.Empty;
            auraTotalText.text = player != null ? player.Wallet.Total.ToString() : "0";
            grappleMarker.gameObject.SetActive(false);
            if (boundCommentator == null)
            {
                Debug.LogWarning($"{name}: no alter ego found yet; subtitles start when one is loaded.", this);
            }
        }

        private void BindCommentator(AlterEgoCommentator target)
        {
            if (target == null || target == boundCommentator)
            {
                return;
            }
            UnbindCommentator();
            boundCommentator = target;
            boundCommentator.LineSpoken += ShowAlterEgoLine;
            alterEgoNameText.text = $"{boundCommentator.Lines.speakerName.ToUpperInvariant()}  ·  alter ego";
        }

        private void UnbindCommentator()
        {
            if (boundCommentator == null)
            {
                return;
            }
            boundCommentator.LineSpoken -= ShowAlterEgoLine;
            boundCommentator = null;
        }

        private void Update()
        {
            if (player == null)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            float now = Time.unscaledTime;
            healthFill.localScale = new Vector3(player.Health.Normalized, 1f, 1f);
            UpdateCombo(dt);
            UpdateGrappleMarker();

            alterEgoGroup.alpha = Mathf.MoveTowards(alterEgoGroup.alpha, now < alterEgoHideTime ? 1f : 0f, dt * 4f);
            if (now >= auraChangeHideTime)
            {
                auraChangeText.text = string.Empty;
            }
            if (now >= centerMessageHideTime)
            {
                centerMessageText.text = string.Empty;
            }
            if (player.Input.ToggleHelpPressed)
            {
                helpPanel.SetActive(!helpPanel.activeSelf);
            }
        }

        private void Bind(PlayerContext context)
        {
            if (context == player)
            {
                return;
            }
            Unbind();
            player = context;
            player.Wallet.Changed += HandleAuraChanged;
            player.Combo.Broken += HandleComboBroken;
            player.Respawner.Downed += HandleDowned;
        }

        private void Unbind()
        {
            if (player == null)
            {
                return;
            }
            player.Wallet.Changed -= HandleAuraChanged;
            player.Combo.Broken -= HandleComboBroken;
            player.Respawner.Downed -= HandleDowned;
            player = null;
        }

        private void UpdateCombo(float dt)
        {
            var combo = player.Combo;
            comboGroup.alpha = Mathf.MoveTowards(comboGroup.alpha, combo.IsActive ? 1f : 0f, dt * 6f);
            if (!combo.IsActive)
            {
                return;
            }
            comboHitsText.text = $"{combo.Hits} VURUŞ";
            // Below the minimum the combo would pay nothing, so do not promise Aura yet.
            comboPointsText.text = combo.QualifiesForPayout
                ? $"+{combo.PendingAward} AURA   x{combo.Multiplier:0.0}"
                : $"<color=#9AA0A6>en az {combo.Settings.minHitsToAward} vuruş</color>";
            comboTimerFill.localScale = new Vector3(combo.TimeLeftNormalized, 1f, 1f);
        }

        private void UpdateGrappleMarker()
        {
            var movement = player.Movement;
            var point = movement.ActiveGrapple != null ? movement.ActiveGrapple : movement.GrappleCandidate;
            if (point == null)
            {
                grappleMarker.gameObject.SetActive(false);
                return;
            }

            Vector3 screen = player.Camera.ViewCamera.WorldToScreenPoint(point.transform.position);
            bool visible = screen.z > 0f;
            grappleMarker.gameObject.SetActive(visible);
            if (visible)
            {
                grappleMarker.position = new Vector3(screen.x, screen.y, 0f);
            }
        }

        private void ShowAlterEgoLine(string line, AuraTier tier)
        {
            alterEgoLineText.text = line;
            alterEgoLineText.color = tier switch
            {
                AuraTier.Panic => panicColor,
                AuraTier.Uneasy => uneasyColor,
                _ => calmColor,
            };
            alterEgoHideTime = Time.unscaledTime + boundCommentator.Lines.lineDuration;
        }

        private void HandleAuraChanged(int total, int delta)
        {
            auraTotalText.text = total.ToString();
            ShowAuraChange($"+{delta}", auraGainColor);
            if (levelComplete)
            {
                ShowLevelComplete();
            }
        }

        private void HandleComboBroken(ComboBreakReason reason, int lostPoints)
        {
            if (lostPoints > 0)
            {
                ShowAuraChange($"KOMBO BOZULDU  -{lostPoints}", auraLossColor);
            }
        }

        private void ShowAuraChange(string text, Color color)
        {
            auraChangeText.text = text;
            auraChangeText.color = color;
            auraChangeHideTime = Time.unscaledTime + 1.8f;
        }

        private void HandleDowned(RespawnReason reason)
        {
            if (levelComplete)
            {
                return;
            }
            centerMessageText.text = reason == RespawnReason.FellIntoVoid ? "BOŞLUĞA DÜŞTÜN" : "UYANMAK YOK, DEVAM";
            centerMessageHideTime = Time.unscaledTime + 1.6f;
        }

        private void HandleLevelGoal(LevelGoal goal)
        {
            levelComplete = true;
            ShowLevelComplete();
        }

        /// <summary>Stays on screen and follows the wallet, so the running combo paid out at the goal is included.</summary>
        private void ShowLevelComplete()
        {
            int total = player != null ? player.Wallet.Total : 0;
            centerMessageText.text = $"TUVALET BULUNDU!\nToplam Aura: {total}";
            centerMessageHideTime = float.PositiveInfinity;
        }
    }
}
