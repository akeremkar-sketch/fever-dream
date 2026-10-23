using System;
using System.Collections.Generic;
using FeverDream.Combat;
using FeverDream.Core;
using FeverDream.Enemies;
using FeverDream.Player;
using UnityEngine;

namespace FeverDream.Aura
{
    /// <summary>
    /// Counts the running combo and turns it into Aura Points (GDD 1.4): points pile up while hits keep landing,
    /// are paid out in one go when the combo times out, and are lost if the player is hit or falls into the void.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerContext), typeof(AuraWallet))]
    public sealed class ComboTracker : MonoBehaviour
    {
        private const string PerfectDodgeId = "perfect_dodge";

        [SerializeField] private AuraSettings settings;

        private readonly HashSet<string> uniqueMoves = new HashSet<string>();
        private PlayerContext context;
        private AuraWallet wallet;
        private float lastActivityTime;
        private int perfectDodges;

        /// <summary>Hits or pending points changed.</summary>
        public event Action Changed;
        public event Action<ComboSummary> Ended;
        /// <summary>Reason and the Aura Points that were lost.</summary>
        public event Action<ComboBreakReason, int> Broken;

        public AuraSettings Settings => settings;
        public int Hits { get; private set; }
        public float PendingPoints { get; private set; }
        public int UniqueMoves => uniqueMoves.Count;
        public bool IsActive => Hits > 0 || PendingPoints > 0f;
        /// <summary>Whether the running combo would pay anything if it ended now (minimum hits, or a perfect dodge).</summary>
        public bool QualifiesForPayout => Hits >= settings.minHitsToAward || perfectDodges > 0;
        public float Multiplier => Mathf.Min(settings.maxMultiplier, 1f + settings.varietyPerUniqueMove * Mathf.Max(0, uniqueMoves.Count - 1));
        public int PendingAward => Mathf.RoundToInt(PendingPoints * Multiplier);
        public float TimeLeftNormalized => IsActive ? Mathf.Clamp01(1f - (Time.time - lastActivityTime) / settings.comboTimeout) : 0f;

        private void Awake()
        {
            context = GetComponent<PlayerContext>();
            wallet = GetComponent<AuraWallet>();
            if (settings == null)
            {
                Debug.LogError($"{name}: ComboTracker has no AuraSettings assigned.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            context.DamageReceiver.Damaged += HandleDamaged;
            context.Respawner.Downed += HandleDowned;
            LevelGoal.Reached += HandleLevelGoal;
            EnemyBase.AnyKilled += HandleEnemyKilled;
        }

        private void OnDisable()
        {
            context.DamageReceiver.Damaged -= HandleDamaged;
            context.Respawner.Downed -= HandleDowned;
            LevelGoal.Reached -= HandleLevelGoal;
            EnemyBase.AnyKilled -= HandleEnemyKilled;
        }

        private void Update()
        {
            if (IsActive && Time.time - lastActivityTime > settings.comboTimeout)
            {
                End();
            }
        }

        public void RegisterHit(ComboMove move, AttackFlags flags, bool killed)
        {
            float points = move.auraValue
                + Bonus(flags, AttackFlags.Aerial, settings.aerialHitBonus)
                + Bonus(flags, AttackFlags.Launcher, settings.launcherBonus)
                + Bonus(flags, AttackFlags.Finisher, settings.finisherBonus)
                + Bonus(flags, AttackFlags.Slam, settings.slamBonus)
                + (killed ? settings.killBonus : 0f);

            Hits++;
            uniqueMoves.Add(move.id);
            PendingPoints += points;
            lastActivityTime = Time.time;
            Changed?.Invoke();
        }

        public void RegisterPerfectDodge()
        {
            perfectDodges++;
            uniqueMoves.Add(PerfectDodgeId);
            PendingPoints += settings.perfectDodgeBonus;
            lastActivityTime = Time.time;
            Changed?.Invoke();
        }

        public void Break(ComboBreakReason reason)
        {
            if (!IsActive)
            {
                return;
            }
            int lost = QualifiesForPayout ? PendingAward : 0;
            Clear();
            Broken?.Invoke(reason, lost);
            Changed?.Invoke();
        }

        private void End()
        {
            // A perfect dodge is style on its own: it pays out even without the minimum number of hits.
            int award = QualifiesForPayout ? PendingAward : 0;
            var summary = new ComboSummary(Hits, uniqueMoves.Count, Multiplier, award);
            Clear();
            wallet.Add(award);
            Ended?.Invoke(summary);
            Changed?.Invoke();
        }

        private void Clear()
        {
            Hits = 0;
            PendingPoints = 0f;
            perfectDodges = 0;
            uniqueMoves.Clear();
        }

        /// <summary>Reaching the goal pays out the running combo right away, so the level total is final.</summary>
        private void HandleLevelGoal(LevelGoal goal)
        {
            if (IsActive)
            {
                End();
            }
        }

        /// <summary>Knocking an enemy into the void during a combo is a kill too (sword kills are counted in RegisterHit).</summary>
        private void HandleEnemyKilled(EnemyBase enemy)
        {
            if (enemy.DiedInVoid && IsActive)
            {
                PendingPoints += settings.killBonus;
                lastActivityTime = Time.time;
                Changed?.Invoke();
            }
        }

        private static float Bonus(AttackFlags flags, AttackFlags flag, float bonus) => (flags & flag) != 0 ? bonus : 0f;

        private void HandleDamaged(DamageInfo info) => Break(ComboBreakReason.Damaged);

        private void HandleDowned(RespawnReason reason) =>
            Break(reason == RespawnReason.FellIntoVoid ? ComboBreakReason.FellIntoVoid : ComboBreakReason.Died);
    }
}
