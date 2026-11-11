using System;
using FeverDream.Core;
using UnityEngine;

namespace FeverDream.Player
{
    /// <summary>
    /// First-person parkour movement on a CharacterController: walk, sprint, crouch, slide, jump, wall run,
    /// wall jump, ledge climb (mantle) and the Zincirli Kama grapple. All tuning lives in <see cref="PlayerMovementSettings"/>.
    /// The transform sits at the feet; the capsule grows upwards from there.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController), typeof(PlayerInputReader))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        private const float WallNormalMaxY = 0.3f;
        private const float WalkableNormalMinY = 0.7f;
        private const float MantleRiseSplit = 0.55f;

        [SerializeField] private PlayerMovementSettings settings;
        [Tooltip("Eye / camera pivot. Grapple targets are picked from its view direction.")]
        [SerializeField] private Transform viewPoint;

        private CharacterController controller;
        private PlayerInputReader input;
        private float standStepOffset;
        private Vector3 velocity;
        private MovementState state = MovementState.Airborne;
        private float stateTime;
        private float coyoteTimer;
        private float jumpBufferTimer;
        private float slideBufferTimer;
        private bool justLanded;
        private bool isCrouched;
        private CollisionFlags lastCollision;

        private Vector3 wallNormal;
        private Collider currentWall;
        /// <summary>The wall last run along or kicked off. It (and any wall facing the same way) cannot be used again until the player touches the ground.</summary>
        private Collider lastWall;
        private Vector3 lastWallNormal;
        private int wallSide;

        private Vector3 mantleStart;
        private Vector3 mantleMid;
        private Vector3 mantleEnd;

        private GrapplePoint grappleTarget;

        private Vector3 lungeDirection;
        private float lungeSpeed;
        private float lungeTimer;
        private bool lungeApplied;

        public event Action<MovementState, MovementState> StateChanged;
        public event Action Jumped;
        public event Action Landed;

        public PlayerMovementSettings Settings => settings;
        public MovementState State => state;
        public Vector3 Velocity => velocity;
        public float HorizontalSpeed => Flat(velocity).magnitude;
        public bool IsGrounded => state == MovementState.Grounded || state == MovementState.Sliding;
        public bool IsCrouched => isCrouched;
        public bool IsSprinting => state == MovementState.Grounded && !isCrouched && WantsSprint;
        /// <summary>+1 when running along a wall on the right, -1 on the left, 0 otherwise.</summary>
        public int WallSide => state == MovementState.WallRunning ? wallSide : 0;
        public float EyeHeight => isCrouched || state == MovementState.Sliding ? settings.crouchEyeHeight : settings.standEyeHeight;
        /// <summary>Current capsule height. The camera never rises above it, so crouching never clips the view into a ceiling.</summary>
        public float CapsuleHeight => controller != null ? controller.height : settings.standHeight;
        public GrapplePoint GrappleCandidate { get; private set; }
        public GrapplePoint ActiveGrapple => state == MovementState.Grappling ? grappleTarget : null;

        private bool WantsSprint => input.SprintHeld && input.Move.y > 0.1f;
        private bool PushingForward => input.Move.y > 0.1f;
        private float JumpSpeed => Mathf.Sqrt(2f * settings.jumpHeight * -settings.gravity);

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputReader>();
            if (settings == null)
            {
                Debug.LogError($"{name}: PlayerMovement has no PlayerMovementSettings assigned.", this);
                enabled = false;
                return;
            }
            standStepOffset = controller.stepOffset;
            ApplyHeight(settings.standHeight);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            // On the frame the game pauses, deltaTime is still the old value but input already reads as idle;
            // ticking it would end a wall run or a slide behind the pause menu.
            if (dt <= 0f || GamePause.IsPaused)
            {
                return;
            }

            stateTime += dt;
            coyoteTimer -= dt;
            jumpBufferTimer = input.JumpPressed ? settings.jumpBufferTime : jumpBufferTimer - dt;
            slideBufferTimer = input.SlidePressed ? settings.jumpBufferTime : slideBufferTimer - dt;
            UpdateGrappleCandidate();

            switch (state)
            {
                case MovementState.Grounded: TickGrounded(dt); break;
                case MovementState.Airborne: TickAirborne(dt); break;
                case MovementState.Sliding: TickSliding(dt); break;
                case MovementState.WallRunning: TickWallRun(); break;
                case MovementState.Mantling: TickMantle(); break;
                case MovementState.Grappling: TickGrapple(dt); break;
            }

            if (state != MovementState.Mantling)
            {
                Move(dt);
            }
            // Aged after use, so a lunge started this frame is applied at least once even on a long frame.
            lungeTimer -= dt;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // Drop the part of the velocity that pushes into a wall, so speed checks (wall run, slide) see real motion.
            if (Mathf.Abs(hit.normal.y) > WallNormalMaxY)
            {
                return;
            }
            Vector3 normal = Flat(hit.normal).normalized;
            float intoWall = Vector3.Dot(velocity, normal);
            if (intoWall < 0f)
            {
                velocity -= normal * intoWall;
            }
        }

        #region Public API

        public void AddImpulse(Vector3 impulse)
        {
            if (state == MovementState.Mantling)
            {
                return;
            }
            velocity += impulse;
            if (impulse.y > 0.01f && IsGrounded)
            {
                SetState(MovementState.Airborne);
            }
        }

        public void SetHorizontalVelocity(Vector3 horizontal)
        {
            if (state == MovementState.Mantling || state == MovementState.Grappling)
            {
                return;
            }
            velocity.x = horizontal.x;
            velocity.z = horizontal.z;
        }

        public void SetVerticalVelocity(float vertical)
        {
            if (state == MovementState.Mantling)
            {
                return;
            }
            velocity.y = vertical;
            if (vertical > 0.01f && IsGrounded)
            {
                SetState(MovementState.Airborne);
            }
        }

        /// <summary>Short forward step on the ground: holds at least the lunge speed for its duration, on top of normal movement.</summary>
        public void Lunge(Vector3 direction, float distance, float duration)
        {
            if (state == MovementState.Mantling || state == MovementState.Grappling || distance <= 0f || duration <= 0f)
            {
                return;
            }
            lungeDirection = Flat(direction).normalized;
            lungeSpeed = distance / duration;
            lungeTimer = duration;
        }

        /// <summary>Stops a running lunge, e.g. when a dodge takes over the horizontal velocity.</summary>
        public void CancelLunge()
        {
            lungeTimer = 0f;
            lungeApplied = false;
        }

        /// <summary>Keeps the player floating a little on aerial hits so air combos can continue.</summary>
        public void ApplyAirHover()
        {
            if (state == MovementState.Airborne)
            {
                velocity.y = Mathf.Max(velocity.y, settings.airAttackHoverSpeed);
            }
        }

        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            controller.enabled = true;
            velocity = Vector3.zero;
            grappleTarget = null;
            lastWall = null;
            CancelLunge();
            ApplyCrouch(false);
            SetState(MovementState.Airborne);
        }

        #endregion

        #region States

        private void TickGrounded(float dt)
        {
            coyoteTimer = settings.coyoteTime;
            lastWall = null;
            bool landedThisFrame = justLanded;
            justLanded = false;

            if (TryStartGrapple() || TryStartMantle())
            {
                return;
            }

            // A slide pressed just before landing, or held through the landing, still becomes a slide.
            bool slideRequested = slideBufferTimer > 0f || (landedThisFrame && input.SlideHeld);
            if (slideRequested && !isCrouched && HorizontalSpeed >= settings.slideMinSpeed)
            {
                StartSlide();
                return;
            }

            bool wantsCrouch = input.SlideHeld;
            if (wantsCrouch != isCrouched && (wantsCrouch || CanStand()))
            {
                ApplyCrouch(wantsCrouch);
            }

            if (jumpBufferTimer > 0f && CanStand())
            {
                ApplyCrouch(false);
                DoJump();
                return;
            }

            float speed = isCrouched ? settings.crouchSpeed : WantsSprint ? settings.sprintSpeed : settings.walkSpeed;
            ApplyGroundMovement(dt, speed);
        }

        private void TickAirborne(float dt)
        {
            if (controller.height < settings.standHeight - 0.01f && CanStand())
            {
                ApplyCrouch(false);
            }
            if (TryStartGrapple())
            {
                return;
            }
            if (jumpBufferTimer > 0f)
            {
                if (coyoteTimer > 0f)
                {
                    DoJump();
                    return;
                }
                if (TryWallJump())
                {
                    return;
                }
            }
            if (TryStartMantle() || TryStartWallRun())
            {
                return;
            }

            ApplyAirMovement(dt);
        }

        private void StartSlide()
        {
            // The boost never lifts speed above slideMaxSpeed, so chaining slide-jumps cannot build speed forever.
            Vector3 horizontal = Flat(velocity);
            float speed = horizontal.magnitude;
            float boosted = Mathf.Max(speed, Mathf.Min(speed + settings.slideBoost, settings.slideMaxSpeed));
            horizontal = horizontal.normalized * boosted;
            velocity.x = horizontal.x;
            velocity.z = horizontal.z;
            slideBufferTimer = 0f;
            isCrouched = false;
            ApplyHeight(settings.crouchHeight);
            SetState(MovementState.Sliding);
        }

        private void TickSliding(float dt)
        {
            justLanded = false;
            if (TryStartGrapple())
            {
                return;
            }
            if (jumpBufferTimer > 0f && CanStand())
            {
                ApplyCrouch(false);
                DoJump();
                return;
            }

            Vector3 horizontal = Flat(velocity);
            float speed = Mathf.Max(0f, horizontal.magnitude - settings.slideFriction * dt);
            Vector3 direction = horizontal.sqrMagnitude > 0.0001f ? horizontal.normalized : Flat(transform.forward).normalized;
            Vector3 wish = WishDirection();
            if (wish.sqrMagnitude > 0.01f)
            {
                direction = Vector3.RotateTowards(direction, wish.normalized, settings.slideSteerRate * dt, 0f);
            }
            horizontal = direction * speed;
            velocity.x = horizontal.x;
            velocity.z = horizontal.z;

            bool released = !input.SlideHeld && stateTime > settings.slideMinDuration;
            if (speed < settings.slideExitSpeed || stateTime > settings.slideMaxDuration || released)
            {
                SetState(MovementState.Grounded);
                ApplyCrouch(input.SlideHeld || !CanStand());
            }
        }

        private bool TryStartWallRun()
        {
            if (!PushingForward || HorizontalSpeed < settings.wallRunMinSpeed || HasGroundWithin(settings.wallRunMinHeight))
            {
                return false;
            }

            for (int side = 1; side >= -1; side -= 2)
            {
                if (!Probe(transform.right * side, out var hit) || IsUsedWall(hit))
                {
                    continue;
                }
                // Running straight into a wall is a wall jump or a climb, not a wall run.
                if (Mathf.Abs(Vector3.Dot(Flat(velocity).normalized, hit.normal)) > settings.wallRunMaxEntryDot)
                {
                    continue;
                }

                wallNormal = hit.normal;
                currentWall = hit.collider;
                wallSide = side;
                velocity.y = Mathf.Clamp(velocity.y, settings.wallRunMinEntryUpSpeed, Mathf.Max(settings.wallRunMinEntryUpSpeed, settings.wallRunEntryUpSpeed));
                SetState(MovementState.WallRunning);
                return true;
            }
            return false;
        }

        private void TickWallRun()
        {
            if (TryStartGrapple())
            {
                return;
            }
            if (jumpBufferTimer > 0f)
            {
                if (coyoteTimer > 0f)
                {
                    // A late jump right after leaving an edge counts like an on-time one: same lift, no kick off the wall.
                    velocity.y = Mathf.Max(velocity.y, settings.wallRunEntryUpSpeed);
                    jumpBufferTimer = 0f;
                    coyoteTimer = 0f;
                    Jumped?.Invoke();
                }
                else
                {
                    WallJump(wallNormal, currentWall);
                    return;
                }
            }

            bool onWall = Probe(-wallNormal, out var hit);
            if (onWall)
            {
                wallNormal = hit.normal;
                currentWall = hit.collider;
            }
            if (!onWall || !PushingForward || stateTime > settings.wallRunMaxDuration)
            {
                ExitWallRun();
                return;
            }

            Vector3 along = Vector3.Cross(wallNormal, Vector3.up).normalized;
            if (Vector3.Dot(along, transform.forward) < 0f)
            {
                along = -along;
            }

            // Running into an inside corner: climb it if there is a ledge, otherwise let go instead of pinning to it.
            Vector3 origin = transform.position + Vector3.up * (controller.height * 0.5f);
            if (Physics.Raycast(origin, along, controller.radius + 0.25f, settings.environmentMask, QueryTriggerInteraction.Ignore))
            {
                if (!TryStartMantle(along))
                {
                    ExitWallRun();
                }
                return;
            }

            float alongSpeed = Mathf.Max(settings.wallRunSpeed, Vector3.Dot(Flat(velocity), along));
            Vector3 horizontal = along * alongSpeed - wallNormal * settings.wallStickSpeed;
            velocity.x = horizontal.x;
            velocity.z = horizontal.z;
        }

        private void ExitWallRun()
        {
            lastWall = currentWall;
            lastWallNormal = wallNormal;
            velocity += wallNormal;
            SetState(MovementState.Airborne);
        }

        /// <summary>True for the wall last used, and for any wall facing the same way (coplanar blocks of one wall).</summary>
        private bool IsUsedWall(RaycastHit hit)
        {
            return lastWall != null && (hit.collider == lastWall || Vector3.Dot(hit.normal, lastWallNormal) > settings.sameWallNormalDot);
        }

        private bool TryWallJump()
        {
            Vector3 forward = Flat(transform.forward).normalized;
            Vector3 right = Flat(transform.right).normalized;
            for (int i = 0; i < 4; i++)
            {
                Vector3 direction = i switch { 0 => forward, 1 => -forward, 2 => right, _ => -right };
                if (Probe(direction, out var hit) && !IsUsedWall(hit))
                {
                    WallJump(hit.normal, hit.collider);
                    return true;
                }
            }
            return false;
        }

        private void WallJump(Vector3 normal, Collider wall)
        {
            Vector3 along = Vector3.ProjectOnPlane(Flat(velocity), normal) * settings.wallJumpMomentumKeep;
            velocity = along + normal * settings.wallJumpSideSpeed + Vector3.up * settings.wallJumpUpSpeed;
            lastWall = wall;
            lastWallNormal = normal;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            SetState(MovementState.Airborne);
            Jumped?.Invoke();
        }

        private bool TryStartMantle() => TryStartMantle(Flat(transform.forward).normalized);

        private bool TryStartMantle(Vector3 forward)
        {
            if (!PushingForward)
            {
                return false;
            }

            Vector3 feet = transform.position;
            Vector3 chest = feet + Vector3.up * (settings.mantleMinHeight * 0.8f);
            if (!Physics.Raycast(chest, forward, out var wallHit, controller.radius + settings.mantleReach, settings.environmentMask, QueryTriggerInteraction.Ignore)
                || Mathf.Abs(wallHit.normal.y) > WallNormalMaxY)
            {
                return false;
            }

            // Look down onto the top of the obstacle. A probe that starts inside a taller wall finds nothing.
            Vector3 probe = wallHit.point - wallHit.normal * (controller.radius + 0.15f);
            probe.y = feet.y + settings.mantleMaxHeight + 0.1f;
            float probeLength = settings.mantleMaxHeight - settings.mantleMinHeight + 0.1f;
            if (!Physics.Raycast(probe, Vector3.down, out var topHit, probeLength, settings.environmentMask, QueryTriggerInteraction.Ignore)
                || topHit.normal.y < WalkableNormalMinY)
            {
                return false;
            }

            Vector3 top = topHit.point;
            float radius = controller.radius;
            if (Physics.CheckCapsule(top + Vector3.up * (radius + 0.05f), top + Vector3.up * (settings.standHeight - radius),
                    radius * 0.95f, settings.environmentMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            mantleStart = feet;
            mantleEnd = top + Vector3.up * 0.02f;
            mantleMid = new Vector3(feet.x, mantleEnd.y + 0.15f, feet.z);
            velocity = Vector3.zero;
            controller.enabled = false;
            ApplyCrouch(false);
            SetState(MovementState.Mantling);
            return true;
        }

        private void TickMantle()
        {
            float t = Mathf.Clamp01(stateTime / settings.mantleDuration);
            transform.position = t < MantleRiseSplit
                ? Vector3.Lerp(mantleStart, mantleMid, Smooth(t / MantleRiseSplit))
                : Vector3.Lerp(mantleMid, mantleEnd, Smooth((t - MantleRiseSplit) / (1f - MantleRiseSplit)));

            if (t >= 1f)
            {
                controller.enabled = true;
                velocity = Flat(transform.forward).normalized * settings.mantleExitSpeed;
                SetState(MovementState.Grounded);
            }
        }

        private void UpdateGrappleCandidate()
        {
            GrappleCandidate = null;
            if (state == MovementState.Mantling)
            {
                return;
            }

            Transform eye = viewPoint != null ? viewPoint : transform;
            float bestScore = float.MaxValue;
            var points = GrapplePoint.Active;
            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                if (point == grappleTarget && state == MovementState.Grappling)
                {
                    continue;
                }

                Vector3 toPoint = point.transform.position - eye.position;
                float distance = toPoint.magnitude;
                if (distance < settings.grappleMinDistance || distance > settings.grappleRange)
                {
                    continue;
                }
                float angle = Vector3.Angle(eye.forward, toPoint);
                if (angle > settings.grappleAimAngle
                    || Physics.Raycast(eye.position, toPoint / distance, distance - 0.5f, settings.environmentMask, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                float score = angle + distance * 0.1f;
                if (score < bestScore)
                {
                    bestScore = score;
                    GrappleCandidate = point;
                }
            }
        }

        private bool TryStartGrapple()
        {
            if (!input.GrapplePressed || GrappleCandidate == null)
            {
                return false;
            }

            grappleTarget = GrappleCandidate;
            jumpBufferTimer = 0f;
            lastWall = null;
            if (velocity.y < 0f)
            {
                velocity.y *= settings.grappleFallDamping;
            }
            SetState(MovementState.Grappling);
            return true;
        }

        private void TickGrapple(float dt)
        {
            if (grappleTarget == null || !grappleTarget.isActiveAndEnabled)
            {
                ReleaseGrapple();
                return;
            }
            if (input.GrapplePressed && GrappleCandidate != null)
            {
                // Chain straight into the next point.
                grappleTarget = GrappleCandidate;
                stateTime = 0f;
            }

            Vector3 eye = viewPoint != null ? viewPoint.position : transform.position + Vector3.up * EyeHeight;
            Vector3 toPoint = grappleTarget.transform.position - eye;
            float distance = toPoint.magnitude;
            bool releaseInput = jumpBufferTimer > 0f || (input.GrapplePressed && GrappleCandidate == null && stateTime > settings.grappleReleaseGrace);
            bool blocked = stateTime > settings.grappleBlockedGrace && (lastCollision & CollisionFlags.Sides) != 0;
            if (distance <= settings.grappleReleaseDistance || stateTime > settings.grappleMaxDuration || releaseInput || blocked)
            {
                ReleaseGrapple();
                return;
            }

            velocity = Vector3.MoveTowards(velocity, toPoint / distance * settings.grapplePullSpeed, settings.grappleAcceleration * dt);
        }

        private void ReleaseGrapple()
        {
            velocity *= settings.grappleExitVelocityKeep;
            velocity.y = Mathf.Max(velocity.y, 0f) + settings.grappleExitUpBoost;
            grappleTarget = null;
            jumpBufferTimer = 0f;
            SetState(MovementState.Airborne);
        }

        #endregion

        #region Helpers

        private void Move(float dt)
        {
            // Gravity runs here, after the state tick, so it also acts on the frame of a jump, wall jump or grapple
            // release, and the frame moves with the average of its start and end vertical speed. Together that traces
            // the exact arc, so jump heights and distances are the same at every frame rate.
            float startVerticalSpeed = velocity.y;
            ApplyGravity(dt);
            Vector3 step = velocity * dt;
            step.y = (startVerticalSpeed + velocity.y) * 0.5f * dt;
            lastCollision = controller.Move(step);
            if ((lastCollision & CollisionFlags.Above) != 0 && velocity.y > 0f)
            {
                velocity.y = 0f;
            }

            bool grounded = controller.isGrounded;
            switch (state)
            {
                case MovementState.Airborne:
                    if (grounded && velocity.y <= 0f)
                    {
                        Land();
                    }
                    break;
                case MovementState.Grounded:
                case MovementState.Sliding:
                    if (grounded)
                    {
                        velocity.y = -settings.groundStickSpeed;
                    }
                    else
                    {
                        coyoteTimer = settings.coyoteTime;
                        SetState(MovementState.Airborne);
                    }
                    break;
                case MovementState.WallRunning:
                    if (grounded)
                    {
                        ExitWallRun();
                        Land();
                    }
                    break;
            }
        }

        private void Land()
        {
            justLanded = true;
            lastWall = null;
            SetState(MovementState.Grounded);
            Landed?.Invoke();
        }

        private void ApplyGroundMovement(float dt, float speed)
        {
            Vector3 horizontal = Vector3.MoveTowards(Flat(velocity), WishDirection() * speed, settings.groundAcceleration * dt);
            if (lungeTimer > 0f)
            {
                // Only the lunge time left in this frame counts, so a long frame never overshoots the lunge distance.
                float targetAlong = lungeSpeed * Mathf.Min(1f, lungeTimer / dt);
                float alongLunge = Vector3.Dot(horizontal, lungeDirection);
                if (alongLunge < targetAlong)
                {
                    horizontal += lungeDirection * (targetAlong - alongLunge);
                }
                lungeApplied = true;
            }
            else if (lungeApplied)
            {
                // The lunge is over: drop the speed it added beyond normal movement, so the step ends at its distance.
                lungeApplied = false;
                float alongLunge = Vector3.Dot(horizontal, lungeDirection);
                float wantedAlong = Mathf.Max(0f, Vector3.Dot(WishDirection() * speed, lungeDirection));
                if (alongLunge > wantedAlong)
                {
                    horizontal -= lungeDirection * (alongLunge - wantedAlong);
                }
            }
            velocity.x = horizontal.x;
            velocity.z = horizontal.z;
        }

        private void ApplyAirMovement(float dt)
        {
            Vector3 wish = WishDirection();
            if (wish.sqrMagnitude < 0.01f)
            {
                return;
            }

            // Steer without losing momentum gained from slides, wall jumps or the grapple.
            Vector3 horizontal = Flat(velocity);
            float targetSpeed = Mathf.Max(WantsSprint ? settings.sprintSpeed : settings.walkSpeed, horizontal.magnitude);
            horizontal = Vector3.MoveTowards(horizontal, wish * targetSpeed, settings.airAcceleration * dt);
            velocity.x = horizontal.x;
            velocity.z = horizontal.z;
        }

        /// <summary>Only the airborne and wall-run states fall. Grounded states stick to the floor; the grapple drives the whole velocity.</summary>
        private void ApplyGravity(float dt)
        {
            float gravity = state switch
            {
                MovementState.Airborne => settings.gravity,
                MovementState.WallRunning => settings.wallRunGravity,
                _ => 0f,
            };
            if (gravity != 0f)
            {
                velocity.y = Mathf.Max(velocity.y + gravity * dt, -settings.terminalFallSpeed);
            }
        }

        private void DoJump()
        {
            CancelLunge();
            velocity.y = JumpSpeed;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            SetState(MovementState.Airborne);
            Jumped?.Invoke();
        }

        private Vector3 WishDirection()
        {
            Vector2 move = input.Move;
            Vector3 forward = Flat(transform.forward).normalized;
            Vector3 right = Flat(transform.right).normalized;
            return Vector3.ClampMagnitude(right * move.x + forward * move.y, 1f);
        }

        private bool Probe(Vector3 direction, out RaycastHit hit)
        {
            Vector3 origin = transform.position + Vector3.up * (controller.height * 0.5f);
            return Physics.Raycast(origin, direction, out hit, controller.radius + settings.wallCheckDistance, settings.environmentMask, QueryTriggerInteraction.Ignore)
                && Mathf.Abs(hit.normal.y) < WallNormalMaxY;
        }

        private bool HasGroundWithin(float distance)
        {
            return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, distance + 0.1f, settings.environmentMask, QueryTriggerInteraction.Ignore);
        }

        private bool CanStand()
        {
            float radius = controller.radius;
            Vector3 feet = transform.position;
            return !Physics.CheckCapsule(feet + Vector3.up * (radius + 0.05f), feet + Vector3.up * (settings.standHeight - radius),
                radius * 0.95f, settings.environmentMask, QueryTriggerInteraction.Ignore);
        }

        private void ApplyCrouch(bool crouched)
        {
            isCrouched = crouched;
            ApplyHeight(crouched ? settings.crouchHeight : settings.standHeight);
        }

        private void ApplyHeight(float height)
        {
            controller.height = height;
            controller.center = new Vector3(0f, height * 0.5f, 0f);
            // Before every sideways move the controller lifts itself by its step offset. With the standing step a low
            // capsule rises into the edge of a gap it fits through and snags there, so a low capsule gets a low step.
            bool low = height < settings.standHeight - 0.01f;
            controller.stepOffset = low ? Mathf.Min(settings.crouchStepOffset, standStepOffset) : standStepOffset;
        }

        private void SetState(MovementState next)
        {
            if (next == state)
            {
                return;
            }
            var previous = state;
            state = next;
            stateTime = 0f;
            StateChanged?.Invoke(previous, next);
        }

        private static Vector3 Flat(Vector3 vector) => new Vector3(vector.x, 0f, vector.z);

        private static float Smooth(float t) => t * t * (3f - 2f * t);

        #endregion
    }
}
