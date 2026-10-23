using System;
using System.Collections.Generic;
using FeverDream.Core;
using FeverDream.Player;
using UnityEngine;

namespace FeverDream.Combat
{
    /// <summary>
    /// Tsunagi melee combat. Picks the next move from the current form (ground chain, launcher, finisher,
    /// air chain, air slam), buffers input, traces hits along the view and reports landed hits to the combo tracker.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerContext))]
    public sealed class TsunagiWeapon : MonoBehaviour
    {
        private enum Phase
        {
            Idle,
            Startup,
            Recovery,
        }

        private enum AttackInput
        {
            None,
            Light,
            Heavy,
        }

        [SerializeField] private TsunagiFormData form;
        [Tooltip("View camera. Hits are traced along its forward direction.")]
        [SerializeField] private Transform aimOrigin;
        [SerializeField] private LayerMask hitMask = (1 << 7) | (1 << 10);
        [Tooltip("Geometry that blocks hits (no hitting through walls).")]
        [SerializeField] private LayerMask obstacleMask = 1 << 8;

        private readonly HashSet<IDamageable> struck = new HashSet<IDamageable>();
        private readonly Collider[] overlap = new Collider[24];

        private PlayerContext context;
        private Phase phase;
        private ComboMove current;
        private bool currentIsAerial;
        private float phaseTimer;
        private AttackInput buffered;
        private float bufferTimer;
        private int groundIndex;
        private int airIndex;
        private int lightHitsInChain;
        private float chainExpireTime;

        public event Action<ComboMove> MoveStarted;
        public event Action<ComboMove> MoveHitFrame;
        public event Action<ComboMove, DamageResult, Vector3> TargetHit;

        public TsunagiFormData Form => form;
        public bool IsAttacking => phase != Phase.Idle;

        private void Awake()
        {
            context = GetComponent<PlayerContext>();
        }

        private void Update()
        {
            if (form == null || context.Health.IsDead)
            {
                phase = Phase.Idle;
                buffered = AttackInput.None;
                return;
            }

            float dt = Time.deltaTime;
            ReadInput();
            if (context.Movement.IsGrounded)
            {
                airIndex = 0;
            }

            switch (phase)
            {
                case Phase.Startup:
                    phaseTimer -= dt;
                    if (phaseTimer <= 0f)
                    {
                        PerformHit();
                        phase = Phase.Recovery;
                        phaseTimer = current.recovery;
                    }
                    break;
                case Phase.Recovery:
                    phaseTimer -= dt;
                    if (phaseTimer <= 0f)
                    {
                        phase = Phase.Idle;
                        chainExpireTime = Time.time + current.chainWindow;
                    }
                    break;
            }

            if (phase == Phase.Idle)
            {
                if (Time.time > chainExpireTime)
                {
                    groundIndex = 0;
                    airIndex = 0;
                    lightHitsInChain = 0;
                }
                if (buffered != AttackInput.None && context.Movement.State != MovementState.Mantling)
                {
                    var move = SelectMove(buffered);
                    buffered = AttackInput.None;
                    StartMove(move);
                }
            }

            // Age the buffer only after it had its chance this frame, so a press on a long frame is never lost.
            if (buffered != AttackInput.None)
            {
                bufferTimer -= dt;
                if (bufferTimer <= 0f)
                {
                    buffered = AttackInput.None;
                }
            }
        }

        private void ReadInput()
        {
            if (context.Input.LightAttackPressed)
            {
                Buffer(AttackInput.Light);
            }
            else if (context.Input.HeavyAttackPressed)
            {
                Buffer(AttackInput.Heavy);
            }
        }

        private void Buffer(AttackInput attack)
        {
            buffered = attack;
            bufferTimer = form.inputBufferTime;
        }

        private ComboMove SelectMove(AttackInput attack)
        {
            if (!context.Movement.IsGrounded)
            {
                if (attack == AttackInput.Heavy || form.airChain.Length == 0)
                {
                    airIndex = 0;
                    return form.airSlam;
                }
                var airMove = form.airChain[airIndex % form.airChain.Length];
                airIndex++;
                return airMove;
            }

            if (attack == AttackInput.Heavy)
            {
                bool finisher = lightHitsInChain >= form.finisherAfterLightHits;
                groundIndex = 0;
                lightHitsInChain = 0;
                return finisher ? form.finisher : form.launcher;
            }

            var groundMove = form.groundChain[groundIndex % form.groundChain.Length];
            groundIndex = (groundIndex + 1) % form.groundChain.Length;
            lightHitsInChain++;
            return groundMove;
        }

        private void StartMove(ComboMove move)
        {
            current = move;
            currentIsAerial = !context.Movement.IsGrounded;
            phase = Phase.Startup;
            phaseTimer = move.startup;
            struck.Clear();

            bool slam = (move.flags & AttackFlags.Slam) != 0;
            if (slam && currentIsAerial)
            {
                context.Movement.SetVerticalVelocity(-form.slamDiveSpeed);
            }
            else
            {
                if (move.lunge > 0f)
                {
                    context.Movement.AddImpulse(Flat(aimOrigin.forward).normalized * move.lunge);
                }
                if (currentIsAerial)
                {
                    context.Movement.ApplyAirHover();
                }
            }
            MoveStarted?.Invoke(move);
        }

        private void PerformHit()
        {
            Vector3 origin = aimOrigin.position;
            Vector3 forward = aimOrigin.forward;
            bool slam = (current.flags & AttackFlags.Slam) != 0;
            int count = slam
                ? Physics.OverlapSphereNonAlloc(transform.position, current.radius, overlap, hitMask, QueryTriggerInteraction.Collide)
                : Physics.OverlapCapsuleNonAlloc(origin + forward * 0.3f, origin + forward * current.range, current.radius, overlap, hitMask, QueryTriggerInteraction.Collide);

            AttackFlags flags = current.flags | (currentIsAerial ? AttackFlags.Aerial : AttackFlags.None);
            bool landed = false;
            for (int i = 0; i < count; i++)
            {
                var target = overlap[i].GetComponentInParent<IDamageable>();
                if (target == null || ReferenceEquals(target, context.DamageReceiver) || !struck.Add(target))
                {
                    continue;
                }

                Vector3 point = overlap[i].bounds.center;
                if (Physics.Linecast(origin, point, obstacleMask, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                Vector3 direction = slam ? Flat(point - transform.position) : Flat(forward);
                if (direction.sqrMagnitude < 0.0001f)
                {
                    direction = Flat(transform.forward);
                }

                var info = new DamageInfo
                {
                    Amount = current.damage,
                    Point = point,
                    Direction = direction.normalized,
                    Knockback = current.knockback,
                    LaunchForce = current.launchForce,
                    Flags = flags,
                    Source = gameObject,
                };
                var result = target.TakeDamage(info);
                if (result == DamageResult.Hit || result == DamageResult.Killed)
                {
                    landed = true;
                    context.Combo.RegisterHit(current, flags, result == DamageResult.Killed);
                }
                TargetHit?.Invoke(current, result, point);
            }

            if (landed)
            {
                TimeEffects.HitStop(current.hitStop);
                context.Camera.AddShake(current.cameraShake, 0.12f);
                if (currentIsAerial && !slam)
                {
                    context.Movement.ApplyAirHover();
                }
            }
            MoveHitFrame?.Invoke(current);
        }

        private static Vector3 Flat(Vector3 vector) => new Vector3(vector.x, 0f, vector.z);
    }
}
