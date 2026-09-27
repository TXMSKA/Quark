using System;
using System.Collections.Generic;
using UnityEngine;

namespace Quark
{
    [DisallowMultipleComponent]
    public class Motion : Mod<Prop>
    {
        #region FIELDS

        [SerializeField] private Transform target;

        [Header("Tween")]
        [SerializeField] private Vector3 closedPosition;
        [SerializeField] private Vector3 openPosition = Vector3.right;
        [SerializeField] private Vector3 closedRotation;
        [SerializeField] private Vector3 openRotation;
        [SerializeField, Min(0.01f)] private float duration = 1f;
        [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Response")]
        [SerializeField, Min(0.01f)] private float baseline = 1f;
        [SerializeField, Min(0f)] private float speedGain = 0.25f;
        [SerializeField, Min(0.01f)] private float stealthCap = 0.4f;
        [SerializeField, Min(0f)] private float spring = 150f;
        [SerializeField, Min(0f)] private float damping = 20f;
        [SerializeField, Min(0f)] private float maximumForce = 200f;

        [Header("Obstruction")]
        [SerializeField, Range(0.001f, 0.1f)] private float tolerance = 0.01f;
        [SerializeField, Min(0.02f)] private float obstructionTime = 0.4f;
        [SerializeField, Min(0f)] private float startupTime = 0.2f;
        [SerializeField, Min(0.001f)] private float sweepStep = 0.01f;
        [SerializeField, Min(0.00001f)] private float skin = 0.0005f;

        #endregion

        #region LIFETIME

        public override void Hook(Prop owner)
        {
            base.Hook(owner);
            if (target == null) target = owner.transform;
            body = target.GetComponent<Rigidbody>();
            if (target.GetComponentInParent<Prop>(true) != owner || target.GetComponentInParent<Rigidbody>(true) != body)
            {
                Reject("Target must belong to this Prop and select the body root.");
                return;
            }

            travel = new Travel(target);
            if (!travel.Valid) { Reject(travel.Error); return; }

            if (!travel.Constrained && body != null && !body.isKinematic)
            {
                Reject("A free dynamic body uses Physical; a tween body must be authored kinematic.");
                return;
            }

            if (!travel.Constrained && target.parent != null &&
                Quaternion.Angle(Quaternion.Euler(closedRotation), Quaternion.Euler(openRotation)) > 0.001f)
            {
                var scale = target.parent.lossyScale;
                if (Mathf.Abs(scale.x - scale.y) > 0.0001f || Mathf.Abs(scale.x - scale.z) > 0.0001f)
                {
                    Reject("Rotating tween targets require uniformly scaled parents.");
                    return;
                }
            }

            var geometry = new List<Shape>();
            foreach (var collider in target.GetComponentsInChildren<Collider>(true))
            {
                if (collider.isTrigger || collider.GetComponentInParent<Prop>(true) != owner ||
                    collider.attachedRigidbody != body)
                    continue;

                if (!travel.Constrained && collider is not (BoxCollider or SphereCollider or CapsuleCollider) &&
                    !(collider is MeshCollider mesh && mesh.convex))
                {
                    Reject("Tween colliders must be primitives or convex meshes.");
                    return;
                }

                geometry.Add(new Shape(collider, target));
            }

            shapes = geometry.ToArray();
            fixedStep = body != null || shapes.Length > 0;
            valid = travel.Constrained || ReadTween(out progress);
            if (!valid)
            {
                Reject("Tween target must start on the configured position and rotation path.");
                return;
            }

            physical = target.GetComponent<Physical>();
            if (physical != null) physical.motion = this;
            policy = owner.Get<Lock>();
            rest = Progress;
            intensity = Mathf.Max(0.01f, baseline);
            owner.OnUse += Use;
        }

        public override void Handle()
        {
            if (!Active) { Cancel(); return; }
            if (!fixedStep) Step(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (!Active) { Cancel(); return; }
            if (fixedStep) Step(Time.fixedDeltaTime);
        }

        private void OnDisable() => Cancel();
        private void OnDestroy() => Unhook();

        public override void Unhook()
        {
            if (Owner != null) Owner.OnUse -= Use;
            Cancel();
            if (physical != null && physical.motion == this) physical.motion = null;
            physical = null;
            cause = null;
            valid = false;
            base.Unhook();
        }

        #endregion

        #region API

        public float Progress => travel != null && travel.Constrained ? travel.Progress : progress;
        public bool Running { get; private set; }

        public bool Toggle(Context context)
        {
            if (!Active ||
                (physical != null && physical.Active && physical.Held) ||
                (policy != null && policy.Locked) ||
                context == null || context.Target != Owner ||
                (!ReferenceEquals(context.Source, null) && (context.Source == null || !context.Source.isActiveAndEnabled)) ||
                (!travel.Constrained && !ReadTween(out progress)))
                return false;

            cause = context;
            intensity = Mathf.Max(0.01f, baseline);
            if (context.TryGet(Context.Velocity, out var velocity) &&
                float.IsFinite(velocity.x) && float.IsFinite(velocity.y) && float.IsFinite(velocity.z))
                intensity += Vector3.ProjectOnPlane(velocity, Vector3.up).magnitude * Mathf.Max(0f, speedGain);
            if (context.TryGet(Context.Stealth, out var stealth) && stealth)
                intensity = Mathf.Min(intensity, Mathf.Max(0.01f, stealthCap));

            from = Progress;
            goal = from <= 0.5f ? 1f : 0f;
            best = Mathf.Abs(goal - from);
            elapsed = stalled = 0f;
            obstructed = false;
            Running = best > (travel.Constrained ? tolerance : 0.00001f);
            if (!Running) rest = Progress;
            return true;
        }

        public void Stop()
        {
            var running = Running;
            Running = false;
            rest = Progress;
            if (running && physical != null && physical.Active) physical.Rest(rest, obstructed);
        }

        #endregion

        #region MISC

        private Rigidbody body;
        private Physical physical;
        private Lock policy;
        private Travel travel;
        private Shape[] shapes = Array.Empty<Shape>();
        private readonly List<(Shape shape, Collider other)> pairs = new();
        private Context cause;

        private bool valid;
        private bool fixedStep;
        private bool obstructed;

        private float progress;
        private float from, goal;
        private float rest;
        private float elapsed;
        private float intensity;
        private float best, stalled;

        internal Travel Mechanism => travel;

        internal bool Closed(float epsilon) =>
            Active && (travel.Constrained ? Progress <= epsilon : (ReadTween(out var value) && value <= epsilon));

        internal void Rest(float value, bool blocked)
        {
            rest = value;
            obstructed = blocked;
        }

        internal bool Active =>
            valid && isActiveAndEnabled && Owner != null && Owner.isActiveAndEnabled &&
            target != null && target.gameObject.activeInHierarchy && travel != null && travel.Valid;

        private void Reject(string reason)
        {
            valid = false;
            Debug.LogWarning("Motion: " + reason, this);
        }

        private void Use(Context context)
        {
            if (context != null && context.TryGet(Context.Stage, out var phase) && phase == Context.Phase.Press)
                Toggle(context);
        }

        private void Cancel()
        {
            if (Running) Stop();
        }

        private void Step(float delta)
        {
            if (delta <= 0f) return;
            if (physical != null && physical.Active && (physical.Held || !Running)) return;
            if (Running && cause != null && !ReferenceEquals(cause.Source, null) &&
                (cause.Source == null || !cause.Source.isActiveAndEnabled))
            {
                Stop();
                return;
            }

            if (travel.Constrained)
            {
                if (Running)
                {
                    elapsed += delta;
                    var remaining = Mathf.Abs(goal - Progress);
                    if (remaining <= tolerance)
                    {
                        Stop();
                        rest = goal;
                        if (physical != null && physical.Active) return;
                    }
                    else
                    {
                        if (remaining < best - tolerance * 0.1f)
                        {
                            best = remaining;
                            stalled = 0f;
                        }
                        else if (elapsed > startupTime) stalled += delta;

                        if (stalled >= Mathf.Max(0.02f, obstructionTime))
                        {
                            obstructed = true;
                            Stop();
                            return;
                        }
                    }
                }

                if (!obstructed)
                    travel.Drive(Running ? goal : rest, spring * intensity,
                        damping * Mathf.Sqrt(Mathf.Max(0.01f, intensity)), maximumForce);
                return;
            }

            if (!Running) return;

            elapsed += delta * intensity / Mathf.Max(0.01f, duration * Mathf.Abs(goal - from));
            var time = Mathf.Clamp01(elapsed);
            var amount = time >= 1f ? 1f : Mathf.Clamp01(easing != null ? easing.Evaluate(time) : time);
            var next = Mathf.Lerp(from, goal, amount);

            var blocked = false;
            var safe = shapes.Length == 0 ? next : SafeProgress(progress, next, out blocked);
            Apply(safe);
            progress = safe;

            if (blocked)
            {
                obstructed = true;
                Stop();
            }
            else if (time >= 1f) Stop();
        }

        private bool ReadTween(out float amount)
        {
            amount = 0f;
            var closed = Quaternion.Euler(closedRotation);
            var opened = Quaternion.Euler(openRotation);
            var segment = openPosition - closedPosition;

            if (segment.sqrMagnitude > 0.000001f)
                amount = Vector3.Dot(target.localPosition - closedPosition, segment) / segment.sqrMagnitude;
            else
            {
                var rotation = Quaternion.Inverse(closed) * opened;
                if (rotation.w < 0f) rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);
                rotation.ToAngleAxis(out var angle, out var axis);
                if (angle < 0.001f) return false;

                var current = Quaternion.Inverse(closed) * target.localRotation;
                if (current.w < 0f) current = new Quaternion(-current.x, -current.y, -current.z, -current.w);
                amount = 2f * Mathf.Atan2(Vector3.Dot(new Vector3(current.x, current.y, current.z), axis), current.w) *
                    Mathf.Rad2Deg / angle;
            }

            if (amount < -0.001f || amount > 1.001f) return false;

            amount = Mathf.Clamp01(amount);
            return Vector3.Distance(target.localPosition, Vector3.Lerp(closedPosition, openPosition, amount)) <= 0.001f &&
                Quaternion.Angle(target.localRotation, Quaternion.Slerp(closed, opened, amount)) <= 0.5f;
        }

        private Pose Pose(float amount)
        {
            var position = Vector3.Lerp(closedPosition, openPosition, amount);
            var rotation = Quaternion.Slerp(Quaternion.Euler(closedRotation), Quaternion.Euler(openRotation), amount);
            return target.parent != null
                ? new Pose(target.parent.TransformPoint(position), target.parent.rotation * rotation)
                : new Pose(position, rotation);
        }

        private void Apply(float amount)
        {
            var pose = Pose(amount);
            if (body != null)
            {
                body.MovePosition(pose.position);
                body.MoveRotation(pose.rotation);
            }
            else target.SetPositionAndRotation(pose.position, pose.rotation);
        }

        // Walks the path in substeps no longer than sweepStep and stops at the last one that pushes into nothing.
        private float SafeProgress(float start, float end, out bool blocked)
        {
            blocked = false;
            Physics.SyncTransforms();
            var first = Pose(start);
            var last = Pose(end);

            var reach = 0f;
            foreach (var shape in shapes)
                if (Live(shape.Collider))
                    reach = Mathf.Max(reach, Vector3.Distance(shape.Collider.bounds.center, first.position) +
                        shape.Collider.bounds.extents.magnitude);
            var travelled = Vector3.Distance(first.position, last.position) +
                Quaternion.Angle(first.rotation, last.rotation) * Mathf.Deg2Rad * reach;

            // Every point moves at most `travelled`, so the grown bounds contain the whole swept volume.
            pairs.Clear();
            foreach (var shape in shapes)
            {
                var own = shape.Collider;
                if (!Live(own)) continue;
                var bounds = own.bounds;
                var nearby = Physics.OverlapBox(bounds.center, bounds.extents + Vector3.one * (travelled + skin),
                    Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                foreach (var other in nearby)
                    if (!Owns(other) && !Physics.GetIgnoreLayerCollision(own.gameObject.layer, other.gameObject.layer) &&
                        !Physics.GetIgnoreCollision(own, other))
                        pairs.Add((shape, other));
            }
            if (pairs.Count == 0) return end;

            var count = Mathf.Max(1, Mathf.CeilToInt(travelled / sweepStep));
            var free = start;
            for (var i = 1; i <= count; i++)
            {
                var next = Mathf.Lerp(start, end, (float)i / count);
                if (Pushes(Pose(free), Pose(next)))
                {
                    blocked = true;
                    return free;
                }
                free = next;
            }

            return end;
        }

        // Penetration may shrink or hold within skin, so objects can slide along contacts and leave overlaps.
        private bool Pushes(Pose free, Pose next)
        {
            foreach (var (shape, other) in pairs)
            {
                var moved = shape.At(next);
                if (!Physics.ComputePenetration(shape.Collider, moved.position, moved.rotation,
                    other, other.transform.position, other.transform.rotation, out _, out var depth) || depth <= skin)
                    continue;

                var held = shape.At(free);
                Physics.ComputePenetration(shape.Collider, held.position, held.rotation,
                    other, other.transform.position, other.transform.rotation, out _, out var before);
                if (depth > before + skin) return true;
            }

            return false;
        }

        private bool Owns(Collider collider)
        {
            foreach (var shape in shapes)
                if (shape.Collider == collider) return true;
            return false;
        }

        private static bool Live(Collider collider) =>
            collider != null && collider.enabled && collider.gameObject.activeInHierarchy;

        internal sealed class Travel
        {
            internal Rigidbody Body { get; }
            internal string Error { get; }
            internal bool Constrained { get; }
            internal bool Valid => Error == null &&
                (!Constrained || (Body != null && !Body.isKinematic && (hinge != null || slider != null)));

            private readonly HingeJoint hinge;
            private readonly ConfigurableJoint slider;
            private readonly Rigidbody connected;
            private readonly Vector3 anchor, connectedAnchor;
            private readonly Vector3 axis;
            private readonly float minimum, maximum;

            internal Travel(Transform target)
            {
                Body = target.GetComponent<Rigidbody>();
                var joints = target.GetComponents<Joint>();
                if (joints.Length == 0) return;

                Constrained = true;
                if (joints.Length != 1 || Body == null || Body.isKinematic)
                {
                    Error = "A supported mechanism needs one joint and a dynamic Rigidbody.";
                    return;
                }

                connected = joints[0].connectedBody;
                anchor = joints[0].anchor;
                connectedAnchor = joints[0].connectedAnchor;

                Vector3 direction;
                if (joints[0] is HingeJoint hingeJoint)
                {
                    var limits = hingeJoint.limits;
                    minimum = limits.min;
                    maximum = limits.max;
                    direction = hingeJoint.axis.normalized;
                    if (!hingeJoint.useLimits || maximum - minimum <= 0.001f || direction.sqrMagnitude < 0.5f)
                    {
                        Error = "Hinge limits must define a closed minimum and an open maximum.";
                        return;
                    }

                    if (hingeJoint.useMotor || hingeJoint.useSpring)
                    {
                        Error = "Interactable joints must not use a motor or a spring.";
                        return;
                    }

                    hinge = hingeJoint;
                }
                else if (joints[0] is ConfigurableJoint sliderJoint)
                {
                    var axes = new[] { sliderJoint.xMotion, sliderJoint.yMotion, sliderJoint.zMotion };
                    var index = 0;
                    var count = 0;
                    for (var i = 0; i < axes.Length; i++)
                    {
                        if (axes[i] == ConfigurableJointMotion.Limited)
                        {
                            index = i;
                            count++;
                        }
                        else if (axes[i] != ConfigurableJointMotion.Locked) count += 2;
                    }

                    maximum = sliderJoint.linearLimit.limit;
                    minimum = -maximum;
                    var right = sliderJoint.axis.normalized;
                    var up = Vector3.ProjectOnPlane(sliderJoint.secondaryAxis, right).normalized;
                    if (count != 1 || maximum <= 0.00001f || right.sqrMagnitude < 0.5f || up.sqrMagnitude < 0.5f ||
                        sliderJoint.angularXMotion != ConfigurableJointMotion.Locked ||
                        sliderJoint.angularYMotion != ConfigurableJointMotion.Locked ||
                        sliderJoint.angularZMotion != ConfigurableJointMotion.Locked)
                    {
                        Error = "A slider needs one limited linear axis, two locked axes and locked rotation.";
                        return;
                    }

                    if (Drives(sliderJoint.xDrive) || Drives(sliderJoint.yDrive) || Drives(sliderJoint.zDrive))
                    {
                        Error = "Interactable joints must not use a drive.";
                        return;
                    }

                    slider = sliderJoint;
                    direction = index == 0 ? right
                        : index == 1 ? up
                        : Vector3.Cross(right, up).normalized;
                }
                else
                {
                    Error = "Only HingeJoint and ConfigurableJoint slider mechanisms are supported.";
                    return;
                }

                var world = target.rotation * direction;
                axis = connected != null ? Quaternion.Inverse(connected.rotation) * world : world;
            }

            private Vector3 Axis => connected != null ? connected.rotation * axis : axis;
            private Vector3 Anchor => Body.position + Body.rotation * Vector3.Scale(anchor, Body.transform.lossyScale);
            private Vector3 Reference => connected != null
                ? connected.position + connected.rotation * Vector3.Scale(connectedAnchor, connected.transform.lossyScale)
                : connectedAnchor;
            private float Coordinate => hinge != null ? hinge.angle : Vector3.Dot(Anchor - Reference, Axis);
            internal float Progress => Valid && Constrained ? Mathf.InverseLerp(minimum, maximum, Coordinate) : 0f;

            // Moves the joint with forces, so the solver keeps limits and contacts.
            internal void Drive(float amount, float force, float damper, float maximumForce)
            {
                if (!Valid || !Constrained) return;

                var error = Mathf.Lerp(minimum, maximum, Mathf.Clamp01(amount)) - Coordinate;
                var direction = Axis;
                if (hinge != null)
                {
                    var velocity = Vector3.Dot(Body.angularVelocity -
                        (connected != null ? connected.angularVelocity : Vector3.zero), direction);
                    var torque = direction *
                        Mathf.Clamp(error * Mathf.Deg2Rad * force - velocity * damper, -maximumForce, maximumForce);
                    Body.AddTorque(torque, ForceMode.Force);
                    if (connected != null && !connected.isKinematic)
                        connected.AddTorque(-torque, ForceMode.Force);
                }
                else
                {
                    var velocity = Vector3.Dot(Body.GetPointVelocity(Anchor) -
                        (connected != null ? connected.GetPointVelocity(Reference) : Vector3.zero), direction);
                    var push = direction * Mathf.Clamp(error * force - velocity * damper, -maximumForce, maximumForce);
                    Body.AddForceAtPosition(push, Anchor, ForceMode.Force);
                    if (connected != null && !connected.isKinematic)
                        connected.AddForceAtPosition(-push, Reference, ForceMode.Force);
                }
            }

            private static bool Drives(JointDrive drive) => drive.positionSpring > 0f || drive.positionDamper > 0f;
        }

        private readonly struct Shape
        {
            internal Collider Collider { get; }
            private readonly Vector3 offset;
            private readonly Quaternion rotation;

            internal Shape(Collider collider, Transform target)
            {
                Collider = collider;
                offset = Quaternion.Inverse(target.rotation) * (collider.transform.position - target.position);
                rotation = Quaternion.Inverse(target.rotation) * collider.transform.rotation;
            }

            internal Pose At(Pose pose) => new(pose.position + pose.rotation * offset, pose.rotation * rotation);
        }

        #endregion
    }
}
