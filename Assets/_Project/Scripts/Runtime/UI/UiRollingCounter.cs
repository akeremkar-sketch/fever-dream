using System;
using UnityEngine;
using UnityEngine.UI;

namespace FeverDream.UI
{
    /// <summary>
    /// Arcade score counter on a row of single-digit texts. Rolling to a new value lights up and pops only the digits
    /// that change and slows down for the last ticks. While it rolls the digits sit on a fixed grid, so the number does
    /// not jitter sideways; at rest they close up to the font's own spacing. Owns the position, scale and colour of the
    /// digit texts (the punch of the whole number on landing is a UiImpact, played by whoever listens to Landed).
    /// Unscaled time; waits while its canvas is hidden.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiRollingCounter : MonoBehaviour
    {
        private static readonly string[] DigitText = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        private static readonly int[] PowerOfTen = { 1, 10, 100, 1000, 10000, 100000, 1000000, 10000000, 100000000, 1000000000 };
        private const float MaxStep = 0.1f;

        [Tooltip("One Text per digit, from the units digit leftwards (at most 9). Anchored to the right edge, pivot bottom centre.")]
        [SerializeField] private Text[] digits;
        [SerializeField] private Color restColor = new Color(0.98f, 0.95f, 1f, 1f);
        [SerializeField] private Color hotColor = new Color(0.3f, 0.92f, 1f, 1f);

        [Header("Roll")]
        [SerializeField, Min(0.05f)] private float minRollDuration = 0.35f;
        [SerializeField, Min(0.05f)] private float maxRollDuration = 0.8f;
        [Tooltip("An amount this big or bigger gets the longest roll and lands at full strength.")]
        [SerializeField, Min(1)] private int fullAmount = 400;
        [Tooltip("Landing strength of the smallest amount.")]
        [SerializeField, Range(0f, 1f)] private float minStrength = 0.4f;

        [Header("Digits")]
        [Tooltip("Width of one digit cell while rolling. At least as wide as the widest digit of the font.")]
        [SerializeField, Min(1f)] private float cellPitch = 40f;
        [Tooltip("Added between digits at rest, so the outline of one digit does not cut into the face of the next.")]
        [SerializeField] private float restTracking = 3f;
        [Tooltip("Seconds to open up to the grid when a roll starts and to close up again when it lands.")]
        [SerializeField, Min(0.01f)] private float spreadDuration = 0.08f;
        [Tooltip("How much a digit grows when it changes (0.16 = 16%).")]
        [SerializeField, Min(0f)] private float digitPop = 0.16f;
        [Tooltip("The same for a digit that was not there before (the total crossing 10, 100, 1000...).")]
        [SerializeField, Min(0f)] private float newDigitPop = 0.6f;
        [SerializeField, Min(0.01f)] private float digitPopDuration = 0.12f;

        private readonly float[] advance = new float[10];
        private Canvas canvas;
        /// <summary>Digit each cell shows, -1 for an empty cell.</summary>
        private int[] cellDigit;
        /// <summary>1 when a cell has just changed, cooling down to 0. -1: at rest and already drawn that way.</summary>
        private float[] heat;
        private float[] pop;
        private int shown;
        private int target;
        private int rollFrom;
        private int rollTo;
        private float delay = -1f;
        private float rollTime = -1f;
        private float rollDuration;
        private float strength;
        /// <summary>0: digits at the font's own spacing. 1: digits on the grid.</summary>
        private float spread;
        private bool busy;

        /// <summary>A roll has reached its value. The argument is its strength: minStrength for a small amount, up to 1.</summary>
        public event Action<float> Landed;

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            if (digits == null || digits.Length == 0 || digits.Length >= PowerOfTen.Length)
            {
                Debug.LogError($"{name}: UiRollingCounter needs 1 to {PowerOfTen.Length - 1} digit texts.", this);
                enabled = false;
                return;
            }

            cellDigit = new int[digits.Length];
            heat = new float[digits.Length];
            pop = new float[digits.Length];
            for (int i = 0; i < digits.Length; i++)
            {
                cellDigit[i] = -1;
                digits[i].text = string.Empty;
            }
            ReadAdvances();
            SetImmediate(0);
        }

        private void OnDisable()
        {
            // Skipped while the scene is being torn down and the digit objects are already gone.
            if (cellDigit != null && digits[0] != null)
            {
                SetImmediate(target);
            }
        }

        /// <summary>Shows the value at once, without animation.</summary>
        public void SetImmediate(int value)
        {
            if (cellDigit == null)
            {
                return;
            }
            target = Mathf.Clamp(value, 0, PowerOfTen[digits.Length] - 1);
            delay = -1f;
            rollTime = -1f;
            spread = 0f;
            busy = false;
            Show(target);
            for (int i = 0; i < digits.Length; i++)
            {
                heat[i] = -1f;
                digits[i].color = restColor;
                digits[i].rectTransform.localScale = Vector3.one;
            }
            Layout();
        }

        /// <summary>
        /// Rolls to the value once the delay has passed. The value is remembered right away, so the display cannot get
        /// stuck behind it. A roll that is already running goes on until the delay is over, then continues from
        /// whatever is on screen.
        /// </summary>
        public void RollTo(int value, float startDelay = 0f)
        {
            if (cellDigit == null)
            {
                return;
            }
            if (!isActiveAndEnabled)
            {
                SetImmediate(value);
                return;
            }
            target = Mathf.Clamp(value, 0, PowerOfTen[digits.Length] - 1);
            delay = Mathf.Max(0f, startDelay);
            busy = true;
        }

        private void Update()
        {
            // A hidden canvas (the HUD while paused) makes the counter wait, so a payout is not spent unseen.
            if (!busy || (canvas != null && !canvas.enabled))
            {
                return;
            }
            // Capped, so one long frame (a hitch) does not swallow the whole roll.
            float dt = Mathf.Min(Time.unscaledDeltaTime, MaxStep);

            if (delay >= 0f)
            {
                delay -= dt;
                if (delay < 0f)
                {
                    BeginRoll();
                }
            }
            if (rollTime >= 0f)
            {
                rollTime += dt;
                float left = 1f - Mathf.Clamp01(rollTime / rollDuration);
                // Ease-out cubic: fast at first, then separate, readable last ticks.
                Show(rollFrom + Mathf.RoundToInt((rollTo - rollFrom) * (1f - left * left * left)));
                if (left <= 0f)
                {
                    Land();
                }
            }

            spread = Mathf.MoveTowards(spread, rollTime >= 0f ? 1f : 0f, dt / spreadDuration);
            bool warm = CoolDigits(dt);
            Layout();
            busy = delay >= 0f || rollTime >= 0f || warm || spread > 0f;
        }

        private void BeginRoll()
        {
            if (target == shown && rollTime < 0f)
            {
                return;
            }
            rollFrom = shown;
            rollTo = target;
            float size = Mathf.Clamp01(Mathf.Abs(rollTo - rollFrom) / (float)fullAmount);
            rollDuration = Mathf.Lerp(minRollDuration, maxRollDuration, size);
            strength = Mathf.Lerp(minStrength, 1f, size);
            rollTime = 0f;
        }

        private void Land()
        {
            rollTime = -1f;
            Show(rollTo);
            // The whole number flashes once, so the landing reads even when only the last digit was still moving.
            for (int i = 0; i < digits.Length; i++)
            {
                if (cellDigit[i] >= 0)
                {
                    heat[i] = 1f;
                    pop[i] = digitPop;
                }
            }
            Landed?.Invoke(strength);
        }

        /// <summary>Writes a text only for the cells whose digit changes, so rolling builds no strings.</summary>
        private void Show(int value)
        {
            shown = value;
            for (int i = 0; i < digits.Length; i++)
            {
                int digit = i == 0 || value >= PowerOfTen[i] ? value / PowerOfTen[i] % 10 : -1;
                if (digit == cellDigit[i])
                {
                    continue;
                }
                pop[i] = cellDigit[i] < 0 ? newDigitPop : digitPop;
                heat[i] = digit < 0 ? -1f : 1f;
                cellDigit[i] = digit;
                digits[i].text = digit < 0 ? string.Empty : DigitText[digit];
            }
        }

        /// <summary>Draws every warm cell at its current heat, then cools it for the next frame. True while one is warm.</summary>
        private bool CoolDigits(float dt)
        {
            bool warm = false;
            for (int i = 0; i < digits.Length; i++)
            {
                if (heat[i] < 0f)
                {
                    continue;
                }
                float glow = heat[i] * heat[i];
                float scale = 1f + pop[i] * glow;
                digits[i].color = Color.Lerp(restColor, hotColor, glow);
                digits[i].rectTransform.localScale = new Vector3(scale, scale, 1f);
                heat[i] = heat[i] > 0f ? Mathf.Max(0f, heat[i] - dt / digitPopDuration) : -1f;
                warm = true;
            }
            return warm;
        }

        /// <summary>Right-aligned on this object's right edge: font spacing at spread 0, one cell per digit at spread 1.</summary>
        private void Layout()
        {
            float edge = 0f;
            for (int i = 0; i < digits.Length; i++)
            {
                float width = cellDigit[i] < 0 ? 0f : advance[cellDigit[i]];
                float tight = edge + width * 0.5f;
                float grid = (i + 0.5f) * cellPitch;
                digits[i].rectTransform.anchoredPosition = new Vector2(-Mathf.Lerp(tight, grid, spread), 0f);
                edge += width + restTracking;
            }
        }

        /// <summary>Digit widths of the font, so the number at rest is spaced like ordinary text.</summary>
        private void ReadAdvances()
        {
            Text sample = digits[0];
            Font font = sample.font;
            font.RequestCharactersInTexture("0123456789", sample.fontSize, sample.fontStyle);
            for (int digit = 0; digit < advance.Length; digit++)
            {
                bool found = font.GetCharacterInfo((char)('0' + digit), out CharacterInfo info, sample.fontSize, sample.fontStyle);
                advance[digit] = found && info.advance > 0 ? info.advance : cellPitch;
            }
        }
    }
}
