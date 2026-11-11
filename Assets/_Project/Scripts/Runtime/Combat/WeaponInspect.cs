using System;
using FeverDream.Core;
using FeverDream.Player;
using UnityEngine;

namespace FeverDream.Combat
{
    /// <summary>
    /// Cosmetic weapon inspect: Tsunagi pinwheels around its balance point, is caught upright, shows both flats and is
    /// flicked clean. One press plays one round; while the key stays down the rounds follow each other, as in
    /// Counter-Strike. This component only decides when the inspect may run and what its pose is;
    /// <see cref="WeaponSwingAnimator"/> asks for that pose while no swing plays and stays the one writer of the weapon view.
    /// Attacks, dodges, grapples, mantles and hits end it at once. Scaled time, like the swings.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WeaponSwingAnimator))]
    public sealed class WeaponInspect : MonoBehaviour
    {
        /// <summary>Stored by number on the prefab: give new eases a new number, never renumber or reuse an existing one.</summary>
        private enum Ease
        {
            Linear = 0,
            In = 1,
            Out = 2,
            InOut = 3,
            OutBack = 4,
        }

        [Serializable]
        private struct Key
        {
            [Tooltip("Seconds it takes to reach this pose from the previous one.")]
            [Min(0f)] public float duration;
            [Tooltip("Metres the balance point moves away from where it rests, in the camera's space.")]
            public Vector3 offset;
            [Tooltip("Degrees added to the idle Euler angles. X and Y aim the blade, Z rolls it around its own length. "
                + "Whole turns count: -720 on X is two spins.")]
            public Vector3 euler;
            public Ease moveEase;
            public Ease turnEase;
            [Tooltip("Blade trail while moving into this pose.")]
            public bool trail;

            public Key(float duration, Vector3 offset, Vector3 euler, Ease moveEase, Ease turnEase, bool trail)
            {
                this.duration = duration;
                this.offset = offset;
                this.euler = euler;
                this.moveEase = moveEase;
                this.turnEase = turnEase;
                this.trail = trail;
            }
        }

        [Tooltip("Point the katana turns around, in metres along the blade from the guard. Keeps the spin small enough to fit the view.")]
        [SerializeField, Min(0f)] private float balancePoint = 0.28f;
        [Tooltip("Poses in order, starting from the rest pose. End on the rest pose again (no offset, whole turns only) for a "
            + "seamless finish and loop; anything else is eased home, or into the next round, by the swing animator.")]
        [SerializeField] private Key[] keys =
        {
            new Key(0.12f, new Vector3(0f, -0.04f, 0f), new Vector3(14f, 6f, 12f), Ease.Out, Ease.Out, false),                     // coil
            new Key(0.24f, new Vector3(0.34f, 0.22f, 0.44f), new Vector3(-282f, -78f, 0f), Ease.Out, Ease.Linear, true),           // launch to the apex
            new Key(0.42f, new Vector3(0.16f, 0.05f, 0.06f), new Vector3(-800f, -78f, 0f), Ease.In, Ease.Linear, true),            // pinwheel, catch
            new Key(0.12f, new Vector3(0.14f, 0.09f, 0.06f), new Vector3(-786f, -80f, 0f), Ease.OutBack, Ease.OutBack, false),     // snap upright
            new Key(0.24f, new Vector3(0.13f, 0.1f, 0.05f), new Vector3(-788f, -113f, 0f), Ease.InOut, Ease.InOut, false),         // hold, edge turns to the eye
            new Key(0.32f, new Vector3(-0.14f, 0.01f, 0.04f), new Vector3(-706f, -66f, -12f), Ease.InOut, Ease.InOut, false),      // lay it across the view, first flat
            new Key(0.24f, new Vector3(-0.16f, 0.02f, -0.01f), new Vector3(-704f, -72f, -28f), Ease.Linear, Ease.Linear, false),   // admire
            new Key(0.3f, new Vector3(-0.16f, 0.03f, 0.01f), new Vector3(-708f, -68f, -195f), Ease.InOut, Ease.InOut, false),      // turn over, second flat
            new Key(0.22f, new Vector3(-0.14f, 0.02f, 0.04f), new Vector3(-710f, -64f, -208f), Ease.Linear, Ease.Linear, false),   // admire
            new Key(0.12f, new Vector3(0.19f, -0.18f, 0.05f), new Vector3(-690f, 28f, -270f), Ease.In, Ease.In, true),             // flick, edge first
            new Key(0.14f, new Vector3(0.16f, -0.145f, 0.045f), new Vector3(-695f, 24f, -266f), Ease.OutBack, Ease.OutBack, false), // recoil
            new Key(0.42f, Vector3.zero, new Vector3(-720f, 0f, -360f), Ease.InOut, Ease.InOut, false),                            // settle
        };

        private PlayerContext context;
        private bool playing;
        private float elapsed;
        private int segment;
        private float segmentStart;

        public bool IsPlaying => playing;

        /// <summary>
        /// Whether the last <see cref="Step"/> that returned true began a round: a fresh press, or the next round of a
        /// held key. The swing animator then fades out whatever gap there is to the round's first pose.
        /// </summary>
        public bool RoundBegan { get; private set; }

        private void Awake()
        {
            context = GetComponentInParent<PlayerContext>();
            if (context == null)
            {
                Debug.LogError($"{name}: WeaponInspect needs a PlayerContext on a parent.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            context.Weapon.MoveStarted += HandleMoveStarted;
            context.DamageReceiver.Damaged += HandleDamaged;
            context.Respawner.Downed += HandleDowned;
        }

        private void OnDisable()
        {
            playing = false;
            if (context == null)
            {
                return;
            }
            context.Weapon.MoveStarted -= HandleMoveStarted;
            context.DamageReceiver.Damaged -= HandleDamaged;
            context.Respawner.Downed -= HandleDowned;
        }

        /// <summary>
        /// Advances the inspect by one frame and gives its pose in the weapon view's parent space. False while no inspect
        /// plays: the caller then poses the weapon itself. Only <see cref="WeaponSwingAnimator"/> calls this, once per
        /// LateUpdate in which no swing plays: after every Update, so a dodge or grapple started this frame already blocks it.
        /// </summary>
        public bool Step(float deltaTime, Vector3 restPosition, Vector3 restEuler,
            out Vector3 position, out Quaternion rotation, out bool trailOn)
        {
            position = restPosition;
            rotation = Quaternion.identity;
            trailOn = false;
            RoundBegan = false;
            if (!enabled || Blocked())
            {
                playing = false;
                return false;
            }

            if (playing)
            {
                // A second press is never read while it plays: a restart would yank the blade back out of the spin.
                elapsed += deltaTime;
            }
            else if (context.Input.InspectPressed)
            {
                BeginRound(0f);
            }
            else
            {
                return false;
            }

            SkipFinishedKeys();
            if (segment >= keys.Length && context.Input.InspectHeld)
            {
                // The key is still down: play it again. segmentStart is the length of the whole round here, and the
                // time this frame ran past it carries into the next round, so the loop keeps its pace.
                BeginRound(elapsed - segmentStart);
                SkipFinishedKeys();
            }
            if (segment >= keys.Length)
            {
                // Finished. Also where a round shorter than one frame ends up: it cannot loop.
                playing = false;
                return false;
            }

            Key from = segment > 0 ? keys[segment - 1] : default;
            Key to = keys[segment];
            float t = (elapsed - segmentStart) / to.duration;
            Vector3 offset = Vector3.LerpUnclamped(from.offset, to.offset, Apply(to.moveEase, t));
            // Euler angles are blended per axis, like the swings: a slerp could not hold more than half a turn.
            Vector3 euler = Vector3.LerpUnclamped(from.euler, to.euler, Apply(to.turnEase, t));

            var pivot = new Vector3(0f, 0f, balancePoint);
            rotation = Quaternion.Euler(restEuler + euler);
            position = restPosition + offset + Quaternion.Euler(restEuler) * pivot - rotation * pivot;
            trailOn = to.trail;
            return true;
        }

        private void BeginRound(float startTime)
        {
            playing = true;
            elapsed = startTime;
            segment = 0;
            segmentStart = 0f;
            RoundBegan = true;
        }

        private void SkipFinishedKeys()
        {
            while (segment < keys.Length && elapsed >= segmentStart + keys[segment].duration)
            {
                segmentStart += keys[segment].duration;
                segment++;
            }
        }

        /// <summary>The weapon hand is busy or gone: the states in which TsunagiWeapon and PlayerDodge refuse to act, and their own actions.</summary>
        private bool Blocked()
        {
            MovementState state = context.Movement.State;
            return context.Health.IsDead || context.Weapon.IsAttacking || context.Dodge.IsDashing
                || state == MovementState.Grappling || state == MovementState.Mantling;
        }

        // Step is not called while a swing plays, so without this the inspect would pick up again after the swing.
        private void HandleMoveStarted(ComboMove move) => playing = false;

        private void HandleDamaged(DamageInfo info) => playing = false;

        private void HandleDowned(RespawnReason reason) => playing = false;

        private static float Apply(Ease ease, float t)
        {
            switch (ease)
            {
                case Ease.In:
                    return t * t;
                case Ease.Out:
                    return 1f - (1f - t) * (1f - t) * (1f - t);
                case Ease.InOut:
                    return t * t * (3f - 2f * t);
                case Ease.OutBack:
                    // Runs about 10 % past the pose and comes back: the give of a catch.
                    return 1f + 2.70158f * (t - 1f) * (t - 1f) * (t - 1f) + 1.70158f * (t - 1f) * (t - 1f);
                default:
                    return t;
            }
        }
    }
}
