using UnityEngine;

namespace Quark
{
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody))]
    public class Physical : Mod<Prop>
    {
        #region FIELDS

        [Header("Hand")]
        [SerializeField, Min(0f)] private float force = 100f;
        [SerializeField, Min(0f)] private float damping = 15f;
        [SerializeField, Min(0f)] private float acceleration = 0.5f;
        [SerializeField, Min(0f)] private float maximumForce = 200f;
        [SerializeField, Min(0f)] private float throwImpulse = 8f;

        [Header("Rest")]
        [SerializeField, Min(0f)] private float holdForce = 150f;
        [SerializeField, Min(0f)] private float holdDamping = 20f;
        [SerializeField] private bool snap = true;
        [SerializeField, Range(0f, 0.5f)] private float snapThreshold = 0.08f;
        [SerializeField, Range(0.001f, 0.1f)] private float tolerance = 0.01f;
        [SerializeField, Min(0.02f)] private float obstructionTime = 0.4f;

        #endregion

        #region LIFETIME

        public override void Hook(Prop owner)
        {
            base.Hook(owner);
            body = GetComponent<Rigidbody>();
            travel = new Motion.Travel(transform);
            valid = body != null && !body.isKinematic && GetComponentInParent<Prop>(true) == owner && travel.Valid;
            if (!valid)
            {
                Debug.LogWarning("Physical: " + (travel.Error ?? "needs a dynamic Rigidbody owned by this Prop."), this);
                return;
            }

            Rest(travel.Progress, false);
            owner.OnInteract += Interact;
        }

        private void FixedUpdate()
        {
            if (!Active) { End(); return; }
            if (Held && (!Live(hand.Source) || !Sample())) End();
            if (motion != null && motion.Active && motion.Running) return;

            if (Held)
            {
                var point = transform.TransformPoint(localPoint);
                var velocity = Time.time - sampleTime <= Time.fixedDeltaTime * 2f ? targetVelocity : Vector3.zero;
                var push = (handTarget - point) * force + (velocity - body.GetPointVelocity(point)) * damping;
                if (fresh)
                {
                    push += targetAcceleration * acceleration;
                    fresh = false;
                }

                body.AddForceAtPosition(Vector3.ClampMagnitude(push, Mathf.Max(0f, maximumForce)), point, ForceMode.Force);
                return;
            }

            if (!travel.Constrained || blocked) return;

            if (snapping)
            {
                var remaining = travel.Progress;
                if (remaining <= tolerance)
                {
                    snapping = false;
                    rest = 0f;
                }
                else
                {
                    if (remaining < best - tolerance * 0.1f)
                    {
                        best = remaining;
                        stalled = 0f;
                    }
                    else stalled += Time.fixedDeltaTime;

                    if (stalled >= obstructionTime) { Rest(travel.Progress, true); return; }
                }
            }

            travel.Drive(snapping ? 0f : rest, holdForce, holdDamping, maximumForce);
        }

        private void OnDisable() => Cancel();
        private void OnDestroy() => Unhook();

        public override void Unhook()
        {
            if (Owner != null) Owner.OnInteract -= Interact;
            Cancel();
            valid = false;
            base.Unhook();
        }

        #endregion

        #region API

        public bool Held => hand != null;

        public bool Grab(Context context)
        {
            if (!Active || Held || context == null || context.Target != Owner || !Live(context.Source) ||
                !context.TryGet(Context.Point, out var point) || !Finite(point) ||
                !Read(context, out var view, out var time))
                return false;

            var belongs = false;
            foreach (var collider in body.GetComponentsInChildren<Collider>())
            {
                if (collider.enabled && !collider.isTrigger && collider.attachedRigidbody == body &&
                    collider.GetComponentInParent<Prop>(true) == Owner &&
                    Vector3.Distance(collider.ClosestPoint(point), point) <= 0.02f)
                {
                    belongs = true;
                    break;
                }
            }
            if (!belongs) return false;

            distance = Vector3.Dot(point - view.origin, view.direction);
            if (distance < 0f) return false;

            localPoint = transform.InverseTransformPoint(point);
            handTarget = view.GetPoint(distance);
            targetVelocity = targetAcceleration = Vector3.zero;
            sampleTime = time;
            fresh = false;

            if (motion != null) motion.Stop();
            hand = context;
            snapping = blocked = false;
            Owner.Grab(new Context(context.Source, Owner));
            return true;
        }

        public bool Release(Context context)
        {
            if (!Held || context == null || context.Target != Owner || !ReferenceEquals(context.Source, hand.Source))
                return false;

            End();
            return true;
        }

        #endregion

        #region MISC

        private Rigidbody body;
        private Motion.Travel travel;
        private Context hand;

        private bool valid;
        private bool fresh;
        private bool snapping;
        private bool blocked;

        private Vector3 localPoint;
        private Vector3 handTarget;
        private Vector3 targetVelocity, targetAcceleration;

        private float distance;
        private float sampleTime;
        private float rest;
        private float best, stalled;

        // Set by the Motion that drives this body, whichever of the two hooks first.
        internal Motion motion;

        internal Motion.Travel Mechanism => travel;

        internal bool Active =>
            valid && isActiveAndEnabled && Owner != null && Owner.isActiveAndEnabled &&
            body != null && !body.isKinematic && travel != null && travel.Valid;

        internal float Progress => travel != null ? travel.Progress : 0f;

        internal void Rest(float value, bool obstructed)
        {
            rest = value;
            blocked = obstructed;
            snapping = !obstructed && snap && travel.Constrained && value > tolerance && value <= snapThreshold;
            best = value;
            stalled = 0f;
        }

        private void End()
        {
            if (!Held) return;

            var previous = hand;
            hand = null;
            Rest(travel.Progress, false);
            targetVelocity = targetAcceleration = Vector3.zero;
            fresh = false;
            if (Owner != null) Owner.Release(new Context(previous.Source, Owner));
        }

        // Hands the resting pose to Motion so that it does not pull the body back to an older one.
        private void Cancel()
        {
            End();
            if (motion != null && motion.Active && !motion.Running) motion.Rest(Progress, blocked);
        }

        private void Interact(Context context)
        {
            if (context == null || !context.TryGet(Context.Input, out var channel) ||
                !context.TryGet(Context.Stage, out var phase))
                return;

            if (channel == Context.Channel.Primary)
            {
                if (phase == Context.Phase.Press) Grab(context);
                else Release(context);
            }
            else if (phase == Context.Phase.Press && Active && Held && !travel.Constrained &&
                context.Target == Owner && ReferenceEquals(context.Source, hand.Source) &&
                Read(context, out var view, out _))
            {
                body.AddForce(view.direction * Mathf.Max(0f, throwImpulse), ForceMode.Impulse);
                End();
            }
        }

        private bool Sample()
        {
            if (!Read(hand, out var view, out var time)) return false;
            if (time <= sampleTime) return true;

            var next = view.GetPoint(distance);
            var delta = time - sampleTime;
            var velocity = (next - handTarget) / delta;
            targetAcceleration = Vector3.ClampMagnitude((velocity - targetVelocity) / delta, maximumForce);
            targetVelocity = Vector3.ClampMagnitude(velocity, maximumForce);
            handTarget = next;
            sampleTime = time;
            fresh = true;
            return true;
        }

        private static bool Read(Context context, out Ray view, out float time)
        {
            view = default;
            time = 0f;
            if (!context.TryGet(Context.View, out view) || !context.TryGet(Context.SampleTime, out time) ||
                !Finite(view.origin) || !Finite(view.direction) || !float.IsFinite(time) ||
                view.direction.sqrMagnitude < 0.0001f)
                return false;

            view.direction = view.direction.normalized;
            return true;
        }

        private static bool Live(Host source) => source != null && source.isActiveAndEnabled;

        private static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        #endregion
    }
}
