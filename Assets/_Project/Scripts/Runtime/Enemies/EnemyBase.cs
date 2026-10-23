using System;
using FeverDream.Core;
using FeverDream.Player;
using UnityEngine;

namespace FeverDream.Enemies
{
    /// <summary>
    /// Shared enemy behaviour on a Rigidbody: idle → chase → telegraphed attack → recover, plus stagger,
    /// launch and air juggle reactions so the Tsunagi air combo works on every enemy type.
    /// Subclasses change how they approach (<see cref="TickChase"/>), attack (<see cref="PerformAttack"/>)
    /// and defend (<see cref="IsBlocking"/>).
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Health))]
    public abstract class EnemyBase : MonoBehaviour, IDamageable, IVoidListener
    {
        protected enum State
        {
            Idle,
            Chase,
            Windup,
            Recover,
            Stagger,
            Airborne,
            Dead,
        }

        private const float GroundProbeDistance = 0.35f;
        private const float AttackHeightTolerance = 2.2f;
        /// <summary>Only this many enemies may be winding up or recovering from an attack at once, so the player can keep a combo going.</summary>
        private const int MaxSimultaneousAttackers = 2;

        private static int activeAttackers;

        [SerializeField] private EnemySettings settings;
        [SerializeField] private LayerMask environmentMask = 1 << 8;
        [SerializeField] private EnemyFeedback feedback;

        private State state;
        private float stateTime;
        private float staggerDuration;
        private float airHoldUntil;
        private bool grounded;
        private bool holdsAttackSlot;
        private Quaternion desiredRotation;
        private EnemyArena arena;

        public static event Action<EnemyBase> AnyKilled;

        public EnemySettings Settings => settings;
        public bool IsDead => state == State.Dead;

        protected Rigidbody Body { get; private set; }
        protected Health Health { get; private set; }
        protected Vector3 MoveIntent { get; set; }
        protected float NextAttackTime { get; set; }
        protected State CurrentState => state;
        protected LayerMask EnvironmentMask => environmentMask;

        protected virtual void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Health = GetComponent<Health>();
            arena = GetComponentInParent<EnemyArena>();
            Health.SetMax(settings.maxHealth);
            Body.useGravity = false;
            Body.constraints = RigidbodyConstraints.FreezeRotation;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            if (feedback == null)
            {
                feedback = GetComponent<EnemyFeedback>();
            }
            desiredRotation = transform.rotation;
            NextAttackTime = Time.time + UnityEngine.Random.Range(0.3f, 1.2f);
        }

        protected virtual void Update()
        {
            if (state == State.Dead)
            {
                return;
            }
            stateTime += Time.deltaTime;

            var target = PlayerContext.Current;
            bool hasTarget = target != null && !target.Health.IsDead;
            switch (state)
            {
                case State.Idle:
                    MoveIntent = Vector3.zero;
                    if (hasTarget && CanEngage(target))
                    {
                        SetState(State.Chase);
                    }
                    break;
                case State.Chase:
                    if (hasTarget && CanEngage(target))
                    {
                        TickChase(target);
                    }
                    else
                    {
                        MoveIntent = Vector3.zero;
                        SetState(State.Idle);
                    }
                    break;
                case State.Windup:
                    MoveIntent = Vector3.zero;
                    if (hasTarget)
                    {
                        Face(target.transform.position);
                    }
                    if (stateTime >= settings.attackWindup)
                    {
                        NextAttackTime = Time.time + settings.attackCooldown;
                        if (hasTarget)
                        {
                            PerformAttack(target);
                        }
                        SetState(State.Recover);
                    }
                    break;
                case State.Recover:
                    MoveIntent = Vector3.zero;
                    if (stateTime >= settings.attackRecovery)
                    {
                        SetState(State.Chase);
                    }
                    break;
                case State.Stagger:
                    MoveIntent = Vector3.zero;
                    if (stateTime >= staggerDuration && grounded)
                    {
                        SetState(State.Chase);
                    }
                    break;
                case State.Airborne:
                    MoveIntent = Vector3.zero;
                    if (grounded && stateTime > 0.2f && Body.linearVelocity.y <= 0.05f)
                    {
                        BeginStagger(settings.staggerDuration);
                    }
                    break;
            }

            if (feedback != null)
            {
                feedback.SetTelegraph(state == State.Windup);
            }
        }

        protected virtual void FixedUpdate()
        {
            if (state == State.Dead)
            {
                return;
            }

            float dt = Time.fixedDeltaTime;
            grounded = Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, GroundProbeDistance, environmentMask, QueryTriggerInteraction.Ignore);

            Vector3 velocity = Body.linearVelocity;
            float gravity = settings.gravity;
            if (state == State.Airborne && Time.time < airHoldUntil && velocity.y < 1f)
            {
                gravity *= settings.airHoldGravityScale;
            }
            velocity.y += gravity * dt;

            if (grounded && state != State.Airborne)
            {
                Vector3 intent = MoveIntent;
                if (intent.sqrMagnitude > 0.01f && (!HasGroundAhead(intent.normalized) || !StaysInArena(intent.normalized)))
                {
                    intent = Vector3.zero;
                }
                float acceleration = state == State.Stagger ? settings.groundFriction : settings.acceleration;
                Vector3 planar = Vector3.MoveTowards(new Vector3(velocity.x, 0f, velocity.z), intent, acceleration * dt);
                velocity.x = planar.x;
                velocity.z = planar.z;
            }

            Body.linearVelocity = velocity;
            Body.MoveRotation(Quaternion.RotateTowards(Body.rotation, desiredRotation, settings.turnSpeed * dt));
        }

        public virtual DamageResult TakeDamage(in DamageInfo info)
        {
            if (state == State.Dead)
            {
                return DamageResult.Ignored;
            }
            if (IsBlocking(info))
            {
                if (feedback != null)
                {
                    feedback.PlayBlock();
                }
                return DamageResult.Blocked;
            }

            bool interruptedWindup = state == State.Windup;
            bool killed = Health.ApplyDamage(info.Amount);
            if (feedback != null)
            {
                feedback.PlayHit();
            }
            if (killed)
            {
                Die();
                return DamageResult.Killed;
            }

            ApplyHitReaction(info, interruptedWindup);
            return DamageResult.Hit;
        }

        /// <summary>Falling into the void counts as a defeat: listeners get <see cref="AnyKilled"/> like any other kill.</summary>
        public void OnFellIntoVoid()
        {
            if (state == State.Dead)
            {
                return;
            }
            state = State.Dead;
            ReleaseAttackSlot();
            AnyKilled?.Invoke(this);
            Destroy(gameObject);
        }

        /// <summary>Default melee approach: walk up, then telegraph an attack when in range and facing the player.</summary>
        protected virtual void TickChase(PlayerContext target)
        {
            Vector3 targetPosition = target.transform.position;
            Vector3 toTarget = Flat(targetPosition - transform.position);
            float distance = toTarget.magnitude;
            Face(targetPosition);

            bool inReach = distance <= settings.attackRange && Mathf.Abs(targetPosition.y - transform.position.y) <= AttackHeightTolerance;
            if (inReach && Time.time >= NextAttackTime && IsFacing(targetPosition, 40f) && TryBeginWindup())
            {
                return;
            }
            MoveIntent = distance > settings.attackRange * 0.75f ? toTarget / distance * settings.moveSpeed : Vector3.zero;
        }

        /// <summary>Default melee hit at the end of the wind-up.</summary>
        protected virtual void PerformAttack(PlayerContext target)
        {
            Vector3 targetPosition = target.transform.position;
            Vector3 toTarget = Flat(targetPosition - transform.position);
            float distance = toTarget.magnitude;
            if (!IsFacing(targetPosition, 75f) || Mathf.Abs(targetPosition.y - transform.position.y) > AttackHeightTolerance)
            {
                return;
            }
            if (distance <= settings.attackRange + settings.dodgeLeniency && target.Dodge != null && target.Dodge.TryEvade())
            {
                return;
            }
            if (distance > settings.attackRange + 0.4f)
            {
                return;
            }

            target.DamageReceiver.TakeDamage(new DamageInfo
            {
                Amount = settings.attackDamage,
                Point = targetPosition,
                Direction = toTarget.normalized,
                Knockback = settings.attackKnockback,
                Source = gameObject,
            });
        }

        protected virtual bool IsBlocking(in DamageInfo info) => false;

        /// <summary>Starts the attack telegraph if an attack slot is free. Returns false when too many enemies are already attacking.</summary>
        protected bool TryBeginWindup()
        {
            if (!holdsAttackSlot)
            {
                if (activeAttackers >= MaxSimultaneousAttackers)
                {
                    return false;
                }
                activeAttackers++;
                holdsAttackSlot = true;
            }
            MoveIntent = Vector3.zero;
            SetState(State.Windup);
            return true;
        }

        protected void Face(Vector3 worldPosition)
        {
            Vector3 toTarget = Flat(worldPosition - transform.position);
            if (toTarget.sqrMagnitude > 0.01f)
            {
                desiredRotation = Quaternion.LookRotation(toTarget);
            }
        }

        protected bool IsFacing(Vector3 worldPosition, float maxAngle)
        {
            Vector3 toTarget = Flat(worldPosition - transform.position);
            return toTarget.sqrMagnitude < 0.01f || Vector3.Angle(transform.forward, toTarget) <= maxAngle;
        }

        protected bool HasLineOfSight(PlayerContext target)
        {
            Vector3 eye = transform.position + Vector3.up * 1.6f;
            Vector3 targetEye = target.transform.position + Vector3.up * 1.4f;
            return !Physics.Linecast(eye, targetEye, environmentMask, QueryTriggerInteraction.Ignore);
        }

        protected float FlatDistanceTo(Vector3 worldPosition) => Flat(worldPosition - transform.position).magnitude;

        protected static Vector3 Flat(Vector3 vector) => new Vector3(vector.x, 0f, vector.z);

        private void ApplyHitReaction(in DamageInfo info, bool interruptedWindup)
        {
            Vector3 push = Flat(info.Direction).normalized * info.Knockback * (1f - settings.knockbackResistance);
            Vector3 velocity = Body.linearVelocity;

            if (info.LaunchForce > 0f && settings.launchResistance < 1f)
            {
                velocity = push * 0.3f;
                velocity.y = info.LaunchForce * (1f - settings.launchResistance);
                airHoldUntil = Time.time + settings.airHoldDuration;
                SetState(State.Airborne);
            }
            else if (state == State.Airborne || !grounded)
            {
                velocity = push * 0.3f;
                velocity.y = info.Has(AttackFlags.Slam) ? -18f : Mathf.Max(velocity.y, settings.juggleUpSpeed);
                airHoldUntil = Time.time + settings.airHoldDuration;
                SetState(State.Airborne);
            }
            else
            {
                velocity.x = push.x;
                velocity.z = push.z;
                BeginStagger(settings.staggerDuration * (interruptedWindup ? settings.windupInterruptStaggerMultiplier : 1f));
            }
            Body.linearVelocity = velocity;
        }

        /// <summary>Never shortens a stun that is already running (the alarm clock's long weak-point stun survives follow-up hits).</summary>
        private void BeginStagger(float duration)
        {
            float remaining = state == State.Stagger ? staggerDuration - stateTime : 0f;
            staggerDuration = Mathf.Max(duration, remaining);
            SetState(State.Stagger);
        }

        private bool CanEngage(PlayerContext target)
        {
            Vector3 targetPosition = target.transform.position;
            return FlatDistanceTo(targetPosition) <= settings.detectionRange && (arena == null || arena.Contains(targetPosition));
        }

        private bool StaysInArena(Vector3 direction)
        {
            return arena == null || arena.Contains(transform.position + direction * 0.7f + Vector3.up * 0.1f);
        }

        private void Die()
        {
            state = State.Dead;
            ReleaseAttackSlot();
            MoveIntent = Vector3.zero;
            foreach (var hitbox in GetComponentsInChildren<Collider>())
            {
                hitbox.enabled = false;
            }
            Body.isKinematic = true;
            if (feedback != null)
            {
                feedback.SetTelegraph(false);
                feedback.PlayDeath();
            }
            AnyKilled?.Invoke(this);
            Destroy(gameObject, 0.6f);
        }

        private bool HasGroundAhead(Vector3 direction)
        {
            Vector3 probe = transform.position + direction * 0.7f + Vector3.up * 0.5f;
            return Physics.Raycast(probe, Vector3.down, 1.5f, environmentMask, QueryTriggerInteraction.Ignore);
        }

        private void SetState(State next)
        {
            state = next;
            stateTime = 0f;
            if (next != State.Windup && next != State.Recover)
            {
                ReleaseAttackSlot();
            }
        }

        private void ReleaseAttackSlot()
        {
            if (holdsAttackSlot)
            {
                holdsAttackSlot = false;
                activeAttackers = Mathf.Max(0, activeAttackers - 1);
            }
        }

        protected virtual void OnDisable()
        {
            // A disabled enemy must not keep its attack slot (or attack from a stale wind-up when re-enabled).
            if (state == State.Windup || state == State.Recover)
            {
                SetState(State.Idle);
            }
            ReleaseAttackSlot();
        }

        protected virtual void OnDestroy()
        {
            ReleaseAttackSlot();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            AnyKilled = null;
            activeAttackers = 0;
        }
    }
}
