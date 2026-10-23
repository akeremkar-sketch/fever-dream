using System;
using System.Collections.Generic;
using FeverDream.Aura;
using FeverDream.Core;
using FeverDream.Enemies;
using FeverDream.Player;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FeverDream.AlterEgo
{
    /// <summary>
    /// The sarcastic pigeon on the rocket saucer. Never touches gameplay (GDD 1.2): it only listens to events
    /// and speaks a line, whose tone shifts from sarcastic to panicking as Jack's total Aura grows.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AlterEgoCommentator : MonoBehaviour
    {
        [SerializeField] private AlterEgoLines lines;

        private readonly Dictionary<AlterEgoEvent, int> lastLineIndex = new Dictionary<AlterEgoEvent, int>();
        private PlayerContext player;
        private float nextLineTime;
        private bool levelComplete;

        /// <summary>The active alter ego, so the HUD can find it even when it lives in another (additive) scene.</summary>
        public static AlterEgoCommentator Current { get; private set; }
        public static event Action<AlterEgoCommentator> Registered;

        /// <summary>Line text and the tier it was spoken in.</summary>
        public event Action<string, AuraTier> LineSpoken;

        public AlterEgoLines Lines => lines;

        private void Awake()
        {
            Current = this;
            Registered?.Invoke(this);
        }

        private void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = null;
            Registered = null;
        }

        private void OnEnable()
        {
            PlayerContext.Registered += Bind;
            EnemyBase.AnyKilled += HandleEnemyKilled;
            LevelGoal.Reached += HandleLevelGoal;
            if (PlayerContext.Current != null)
            {
                Bind(PlayerContext.Current);
            }
        }

        private void OnDisable()
        {
            PlayerContext.Registered -= Bind;
            EnemyBase.AnyKilled -= HandleEnemyKilled;
            LevelGoal.Reached -= HandleLevelGoal;
            Unbind();
        }

        private void Start()
        {
            Invoke(nameof(SayLevelStart), lines != null ? lines.levelStartDelay : 1f);
        }

        public void Say(AlterEgoEvent trigger)
        {
            var set = lines != null ? lines.Find(trigger) : null;
            if (set == null || (!set.interrupt && Time.time < nextLineTime) || Random.value > set.chance)
            {
                return;
            }

            AuraTier tier = player != null ? player.Wallet.Tier : AuraTier.Calm;
            string[] pool = tier switch
            {
                AuraTier.Panic => set.panic,
                AuraTier.Uneasy => set.uneasy,
                _ => set.calm,
            };
            if (pool == null || pool.Length == 0)
            {
                pool = set.calm;
            }
            if (pool == null || pool.Length == 0)
            {
                return;
            }

            int index = Random.Range(0, pool.Length);
            if (pool.Length > 1 && lastLineIndex.TryGetValue(trigger, out int last) && index == last)
            {
                index = (index + 1) % pool.Length;
            }
            lastLineIndex[trigger] = index;
            nextLineTime = Time.time + lines.cooldown;
            LineSpoken?.Invoke(pool[index], tier);
        }

        private void SayLevelStart() => Say(AlterEgoEvent.LevelStart);

        private void Bind(PlayerContext context)
        {
            if (context == player)
            {
                return;
            }
            Unbind();
            player = context;
            player.Combo.Ended += HandleComboEnded;
            player.Combo.Broken += HandleComboBroken;
            player.Respawner.Downed += HandleDowned;
            player.Respawner.CheckpointReached += HandleCheckpoint;
            player.Dodge.PerfectDodged += HandlePerfectDodge;
        }

        private void Unbind()
        {
            if (player == null)
            {
                return;
            }
            player.Combo.Ended -= HandleComboEnded;
            player.Combo.Broken -= HandleComboBroken;
            player.Respawner.Downed -= HandleDowned;
            player.Respawner.CheckpointReached -= HandleCheckpoint;
            player.Dodge.PerfectDodged -= HandlePerfectDodge;
            player = null;
        }

        private void HandleComboEnded(ComboSummary summary)
        {
            // The combo paid out at the goal must not talk over the level-complete line.
            if (summary.Points <= 0 || levelComplete)
            {
                return;
            }
            Say(summary.Points >= lines.hugeComboPoints ? AlterEgoEvent.ComboHuge
                : summary.Points >= lines.bigComboPoints ? AlterEgoEvent.ComboBig
                : AlterEgoEvent.ComboSmall);
        }

        private void HandleComboBroken(ComboBreakReason reason, int lostPoints)
        {
            // Falls and deaths have their own lines via Downed.
            if (reason == ComboBreakReason.Damaged && lostPoints > 0)
            {
                Say(AlterEgoEvent.ComboBroken);
            }
        }

        private void HandleDowned(RespawnReason reason) =>
            Say(reason == RespawnReason.FellIntoVoid ? AlterEgoEvent.FellIntoVoid : AlterEgoEvent.PlayerDied);

        private void HandleCheckpoint(Checkpoint checkpoint) => Say(AlterEgoEvent.CheckpointReached);

        private void HandlePerfectDodge() => Say(AlterEgoEvent.PerfectDodge);

        private void HandleEnemyKilled(EnemyBase enemy) => Say(AlterEgoEvent.EnemyKilled);

        private void HandleLevelGoal(LevelGoal goal)
        {
            levelComplete = true;
            Say(AlterEgoEvent.LevelComplete);
        }
    }
}
