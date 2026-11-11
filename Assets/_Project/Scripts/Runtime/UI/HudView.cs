using FeverDream.AlterEgo;
using FeverDream.Aura;
using FeverDream.Core;
using FeverDream.Player;
using UnityEngine;
using UnityEngine.UI;

namespace FeverDream.UI
{
    /// <summary>
    /// In-game HUD. Reads the player through <see cref="PlayerContext.Current"/> and only displays state.
    /// The control list is not shown here; it lives in the pause menu and the main menu.
    /// The HUD hides while the game is paused, so it does not show through the menu.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public sealed class HudView : MonoBehaviour
    {
        [Tooltip("The scene's alter ego. Optional: when empty the HUD uses AlterEgoCommentator.Current (any loaded scene).")]
        [SerializeField] private AlterEgoCommentator commentator;

        [Header("Health")]
        [SerializeField] private UiSegmentedBar healthBar;

        [Header("Combo")]
        [SerializeField] private CanvasGroup comboGroup;
        [SerializeField] private Text comboHitsText;
        [SerializeField] private Text comboPointsText;
        [SerializeField] private RectTransform comboTimerFill;

        [Header("Aura")]
        [SerializeField] private UiRollingCounter auraCounter;
        [Tooltip("The +N that flies into the total when a combo pays out.")]
        [SerializeField] private UiFlyInLabel auraGainLabel;
        [Tooltip("Played when the total lands, with the strength of the payout: number and icon punch, tone flash, burst.")]
        [SerializeField] private UiImpact[] auraLandingEffects;
        [SerializeField] private UiImpact auraTierStamp;
        [SerializeField] private Text auraTierText;
        [Tooltip("Shout when the total reaches a new tier, one per AuraTier. Capitals only: the HUD font has no lower case.")]
        [SerializeField] private string[] auraTierCallouts = { "", "ALEV ALDI!", "AURA TAŞTI!" };
        [Tooltip("Only the combo-lost line now; a gain is shown by the gain label.")]
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

        private PlayerContext player;
        private AlterEgoCommentator boundCommentator;
        private float alterEgoHideTime;
        private float auraChangeHideTime;
        private float centerMessageHideTime;
        /// <summary>Real time the HUD has been visible: unscaled (hit stop does not freeze it) but stopped while paused.</summary>
        private float hudTime;
        private bool levelComplete;
        private AuraTier shownAuraTier;
        private Canvas canvas;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
        }

        private void OnEnable()
        {
            GamePause.Changed += HandlePauseChanged;
            HandlePauseChanged(GamePause.IsPaused);
            PlayerContext.Registered += Bind;
            LevelGoal.Reached += HandleLevelGoal;
            auraCounter.Landed += HandleAuraLanded;
            if (PlayerContext.Current != null)
            {
                Bind(PlayerContext.Current);
            }

            AlterEgoCommentator.Registered += BindCommentator;
            BindCommentator(commentator != null ? commentator : AlterEgoCommentator.Current);
        }

        private void OnDisable()
        {
            GamePause.Changed -= HandlePauseChanged;
            PlayerContext.Registered -= Bind;
            LevelGoal.Reached -= HandleLevelGoal;
            auraCounter.Landed -= HandleAuraLanded;
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
            auraCounter.SetImmediate(player != null ? player.Wallet.Total : 0);
            shownAuraTier = player != null ? player.Wallet.Tier : AuraTier.Calm;
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
            alterEgoNameText.text = TurkishUpper(boundCommentator.Lines.speakerName);
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

            // The HUD is hidden while the game is paused. Its message timers wait too, so a line shown just before
            // pausing is still on screen afterwards.
            if (GamePause.IsPaused)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            hudTime += dt;
            float now = hudTime;
            healthBar.SetValue(player.Health.Normalized);
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
        }

        private void HandlePauseChanged(bool paused) => canvas.enabled = !paused;

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
            alterEgoHideTime = hudTime + boundCommentator.Lines.lineDuration;
        }

        private void HandleAuraChanged(int total, int delta)
        {
            // A gain replaces a "combo lost" line that is still showing in the same spot.
            auraChangeText.text = string.Empty;
            auraGainLabel.Play($"+{delta}", auraGainColor);
            // The counter starts rolling when the label arrives. It is told the real total now, so it cannot get stuck.
            auraCounter.RollTo(total, auraGainLabel.Duration);
            if (levelComplete)
            {
                ShowLevelComplete();
            }
        }

        /// <summary>The rolling total has come to rest: the widget reacts in proportion to the payout.</summary>
        private void HandleAuraLanded(float strength)
        {
            for (int i = 0; i < auraLandingEffects.Length; i++)
            {
                auraLandingEffects[i].Play(strength);
            }

            AuraTier tier = player != null ? player.Wallet.Tier : shownAuraTier;
            if (tier > shownAuraTier && (int)tier < auraTierCallouts.Length)
            {
                auraTierText.text = auraTierCallouts[(int)tier];
                auraTierStamp.Play();
            }
            shownAuraTier = tier;
        }

        private void HandleComboBroken(ComboBreakReason reason, int lostPoints)
        {
            if (lostPoints > 0)
            {
                // The loss line sits where a "+N" may still be showing; the total keeps rolling without the label.
                auraGainLabel.Stop();
                ShowAuraChange($"KOMBO BOZULDU  -{lostPoints}", auraLossColor);
            }
        }

        /// <summary>The HUD lettering font only has capitals; Turkish needs İ for i and I for ı, which ToUpperInvariant gets wrong.</summary>
        private static string TurkishUpper(string text) => text.Replace('i', 'İ').Replace('ı', 'I').ToUpperInvariant();

        private void ShowAuraChange(string text, Color color)
        {
            auraChangeText.text = TurkishUpper(text);
            auraChangeText.color = color;
            auraChangeHideTime = hudTime + 1.8f;
        }

        private void HandleDowned(RespawnReason reason)
        {
            if (levelComplete)
            {
                return;
            }
            centerMessageText.text = reason == RespawnReason.FellIntoVoid ? "BOŞLUĞA DÜŞTÜN" : "UYANMAK YOK, DEVAM";
            centerMessageHideTime = hudTime + 1.6f;
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
