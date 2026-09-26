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
        [SerializeField, Min(0.0001f)] private float sweepStep = 0.01f;
        [SerializeField, Min(0.00001f)] private float skin = 0.0005f;

        [Header("Events")]
        [SerializeField, Min(0.01f)] private float interval = 0.05f;
        [SerializeField, Min(0.01f)] private float speedReference = 1f;

        #endregion

        #region LIFETIME

        public override void Hook(Prop owner)
        {
            base.Hook(owner);
            if (target == null) target = owner.transform;

            foreach (var candidate in owner.GetComponentsInChildren<Motion>(true))
            {
                if (candidate != this && candidate.GetComponentInParent<Prop>(true) == owner &&
                    candidate.Target(owner) == target)
                {
                    Reject("Only one Motion may select a target.");
                    return;
                }
            }

            body = target.GetComponent<Rigidbody>();
            if (target.GetComponentInParent<Prop>(true) != owner || target.GetComponentInParent<Rigidbody>(true) != body)
            {
                Reject("Target must belong to this Prop and select the body root.");
                return;
            }

            physical = target.GetComponent<Physical>();
            policy = owner.Get<Lock>();
            travel = physical != null && physical.Owner == owner && physical.Mechanism != null
                ? physical.Mechanism
                : new Travel(target);
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

                var shape = new Shape(collider, target);
                if (!travel.Constrained && !shape.Valid)
                {
                    Reject("Tween collision requires fixed, unsheared boxes, spheres, capsules or convex meshes.");
                    return;
                }

                geometry.Add(shape);
            }

            shapes = geometry.ToArray();
            fixedStep = body != null || shapes.Length > 0;
            valid = travel.Constrained || ReadTween(out progress);
            if (!valid)
            {
                Reject("Tween target must start on the configured position and rotation path.");
                return;
            }

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
            cause = null;
            valid = false;
            base.Unhook();
        }

        #endregion

        #region API

        public static readonly Key<Endpoint> Limit = new();

        public float Progress => travel != null && travel.Constrained ? travel.Progress : progress;
        public bool Running { get; private set; }

        public bool Open(Context context) => Begin(1f, context);
        public bool Close(Context context) => Begin(0f, context);

        public bool Toggle(Context context)
        {
            if (!Active || (!travel.Constrained && !ReadTween(out progress))) return false;
            return Begin(Progress <= 0.5f ? 1f : 0f, context);
        }

        public void Stop()
        {
            var running = Running;
            Running = false;
            rest = Progress;

            if (running && physical != null && physical.Active)
            {
                Yield();
                physical.Rest(rest, obstructed);
            }
        }

        #endregion

        #region MISC

        private Rigidbody body;
        private Physical physical;
        private Lock policy;
        private bool driving;
        private Travel travel;
        private Shape[] shapes = Array.Empty<Shape>();
        private readonly Observation observation = new();
        private Context cause;

        private bool valid;
        private bool fixedStep;
        private bool observing;
        private bool obstructed;

        private float progress;
        private float from, goal;
        private float rest;
        private float elapsed;
        private float intensity;
        private float best, stalled;

        private Collider[] candidates = new Collider[32];
        private RaycastHit[] hits = new RaycastHit[32];

        internal Travel Mechanism => travel;
        internal Context Cause => cause;
        internal Transform Target(Prop owner) => target != null ? target : owner.transform;

        internal bool Closed(float epsilon) =>
            Active && (travel.Constrained ? Progress <= epsilon : (ReadTween(out var value) && value <= epsilon));

        internal void Rest(float value, bool blocked)
        {
            rest = value;
            obstructed = blocked;
        }

        internal void Yield()
        {
            if (!driving) return;
            travel.Restore();
            driving = false;
        }

        internal void ApplyLock(bool locked)
        {
            if (!Active) return;
            if (locked) Stop();
            if (physical != null && physical.Active) { physical.ApplyLock(locked); return; }

            driving = true;
            travel.Clamp(locked);
        }

        internal bool Active =>
            valid && Enabled && isActiveAndEnabled && Owner != null && Owner.isActiveAndEnabled &&
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

        private bool Begin(float destination, Context context)
        {
            if (!Active ||
                (physical != null && physical.Active && physical.Held) ||
                (policy != null && policy.Locked) ||
                context == null || context.Target != Owner ||
                (!ReferenceEquals(context.Source, null) && (context.Source == null || !context.Source.isActiveAndEnabled)))
                return false;

            if (!travel.Constrained && !ReadTween(out progress)) return false;

            cause = context;
            intensity = Mathf.Max(0.01f, baseline);
            if (context.TryGet(Context.Velocity, out var velocity) &&
                float.IsFinite(velocity.x) && float.IsFinite(velocity.y) && float.IsFinite(velocity.z))
                intensity += Vector3.ProjectOnPlane(velocity, Vector3.up).magnitude * Mathf.Max(0f, speedGain);
            if (context.TryGet(Context.Stealth, out var stealth) && stealth)
                intensity = Mathf.Min(intensity, Mathf.Max(0.01f, stealthCap));

            if (physical != null) physical.Yield();
            from = Progress;
            goal = destination;
            best = Mathf.Abs(goal - from);
            elapsed = stalled = 0f;
            obstructed = false;
            Running = best > (travel.Constrained ? tolerance : 0.00001f);
            if (!Running) rest = Progress;
            return true;
        }

        private void Step(float delta)
        {
            if (delta <= 0f) return;

            if (physical == null || !physical.Active)
            {
                if (!observing)
                {
                    observation.Seed(Progress, target.position, target.rotation, true, tolerance);
                    observing = true;
                }

                observation.Step(Owner, cause, Progress, true, target.position, target.rotation,
                    delta, interval, speedReference, tolerance);
            }
            else observing = false;

            if (policy != null && policy.Locked) { ApplyLock(true); return; }
            if (physical != null && physical.Active && (physical.Held || !Running)) { Yield(); return; }
            if (Running && cause != null && !ReferenceEquals(cause.Source, null) &&
                (cause.Source == null || !cause.Source.isActiveAndEnabled))
            {
                Stop();
                return;
            }

            if (travel.Constrained)
            {
                driving = true;
                travel.Clamp(false);
                travel.Suspend();

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

        private void Cancel()
        {
            if (!Running && !driving && !observing) return;
            Stop();
            Yield();
            observing = false;
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

        private float SafeProgress(float start, float end, out bool blocked)
        {
            blocked = false;
            Physics.SyncTransforms();
            var a = Pose(start);
            var b = Pose(end);

            var radius = 0f;
            var step = Mathf.Max(0.0001f, sweepStep);
            foreach (var shape in shapes)
            {
                radius = Mathf.Max(radius, shape.Radius);
                step = Mathf.Min(step, Mathf.Max(0.0001f, shape.Thickness * 0.25f));
            }

            var distance = Vector3.Distance(a.position, b.position) +
                Quaternion.Angle(a.rotation, b.rotation) * Mathf.Deg2Rad * radius;
            var count = Mathf.Max(1, Mathf.CeilToInt(distance / step));
            if (count > 4096) { blocked = true; return start; }

            var current = start;
            for (var i = 1; i <= count; i++)
            {
                var next = Mathf.Lerp(start, end, (float)i / count);
                var safe = Trace(current, next, 0, out blocked);
                if (blocked) return safe;
                current = next;
            }

            return end;
        }

        private float Trace(float start, float end, int depth, out bool blocked)
        {
            blocked = Blocked(Pose(start), Pose(end));
            if (!blocked) return end;
            if (depth >= 10 || Mathf.Abs(end - start) < 0.000001f) return start;

            var middle = (start + end) * 0.5f;
            var first = Trace(start, middle, depth + 1, out blocked);
            return blocked ? first : Trace(middle, end, depth + 1, out blocked);
        }

        private bool Blocked(Pose a, Pose b)
        {
            foreach (var shape in shapes)
            {
                if (shape.Collider == null || !shape.Collider.enabled || !shape.Collider.gameObject.activeInHierarchy)
                    continue;

                var angle = Quaternion.Angle(a.rotation, b.rotation) * Mathf.Deg2Rad;
                var padding = shape.RotationRadius * 2f * Mathf.Sin(angle * 0.5f) +
                    shape.Radius * (1f - Mathf.Cos(angle * 0.5f));

                int count;
                while ((count = shape.Overlap(a, padding, candidates)) == candidates.Length)
                {
                    if (candidates.Length >= 8192) return true;
                    Array.Resize(ref candidates, candidates.Length * 2);
                }
                for (var i = 0; i < count; i++)
                    if (Blocks(shape, candidates[i], a, b)) return true;

                while ((count = shape.Sweep(a, b, padding, hits)) == hits.Length)
                {
                    if (hits.Length >= 8192) return true;
                    Array.Resize(ref hits, hits.Length * 2);
                }
                for (var i = 0; i < count; i++)
                    if (Blocks(shape, hits[i].collider, a, b)) return true;
            }

            return false;
        }

        private bool Blocks(Shape shape, Collider other, Pose a, Pose b)
        {
            if (other == null || other == shape.Collider || other.isTrigger || !other.enabled ||
                !Collides(shape.Collider, other) || Physics.GetIgnoreCollision(shape.Collider, other))
                return false;

            foreach (var own in shapes)
                if (own.Collider == other) return false;

            var first = shape.At(a);
            var second = shape.At(b);
            var previous = Physics.ComputePenetration(shape.Collider, first.position, first.rotation,
                other, other.transform.position, other.transform.rotation, out var normal, out var oldDepth);
            var next = Physics.ComputePenetration(shape.Collider, second.position, second.rotation,
                other, other.transform.position, other.transform.rotation, out _, out var depth);
            if (next && depth > (previous ? oldDepth : skin)) return true;

            if (previous && oldDepth > 0f && (!next || depth <= oldDepth))
            {
                var oldPoint = Physics.ClosestPoint(other.bounds.center, shape.Collider, first.position, first.rotation);
                var newPoint = second.position +
                    second.rotation * (Quaternion.Inverse(first.rotation) * (oldPoint - first.position));
                if (Vector3.Dot(newPoint - oldPoint, normal) >= 0f) return false;
            }

            if (!next || depth <= skin)
            {
                var center = shape.Center(a);
                var obstaclePoint = other.ClosestPoint(center);
                var outward = center - obstaclePoint;
                if (outward.sqrMagnitude > 0.00000001f)
                {
                    outward.Normalize();
                    var contact = Physics.ClosestPoint(obstaclePoint, shape.Collider, first.position, first.rotation);
                    if (Vector3.Distance(contact, other.ClosestPoint(contact)) <= skin)
                    {
                        var moved = second.position +
                            second.rotation * (Quaternion.Inverse(first.rotation) * (contact - first.position));
                        if (Vector3.Dot(moved - contact, outward) >= 0f) return false;
                    }
                }
            }

            return true;
        }

        private static bool Collides(Collider a, Collider b)
        {
            var standard = !Physics.GetIgnoreLayerCollision(a.gameObject.layer, b.gameObject.layer);
            var first = Decision(a, b.gameObject.layer, standard, out var overrideA);
            var second = Decision(b, a.gameObject.layer, standard, out var overrideB);

            if (first == second) return first;
            if (!overrideA) return second;
            if (!overrideB) return first;

            return a.layerOverridePriority > b.layerOverridePriority
                ? first
                : b.layerOverridePriority > a.layerOverridePriority && second;
        }

        private static bool Decision(Collider collider, int layer, bool standard, out bool overridden)
        {
            var include = (int)collider.includeLayers;
            var exclude = (int)collider.excludeLayers;

            var body = collider.attachedRigidbody;
            if (body != null)
            {
                include |= body.includeLayers;
                exclude |= body.excludeLayers;
            }

            var articulation = collider.attachedArticulationBody;
            if (articulation != null)
            {
                include |= articulation.includeLayers;
                exclude |= articulation.excludeLayers;
            }

            overridden = (include | exclude) != 0;
            var bit = 1 << layer;
            return (standard || (include & bit) != 0) && (exclude & bit) == 0;
        }

        internal sealed class Travel
        {
            internal Rigidbody Body { get; }
            internal string Error { get; }
            internal bool Constrained => kind != 0;
            internal bool Valid => Error == null &&
                (kind == 0 || (Body != null && !Body.isKinematic && (kind == 1 ? hinge != null : slider != null)));

            private readonly int kind;
            private readonly int axisIndex;
            private readonly HingeJoint hinge;
            private readonly ConfigurableJoint slider;
            private readonly Rigidbody connected;
            private readonly Vector3 anchor, connectedAnchor;
            private readonly Vector3 axis;
            private readonly float minimum, maximum;
            private readonly bool motor, spring, limits;
            private readonly bool automatic;
            private readonly JointLimits hingeLimits;
            private readonly SoftJointLimit linearLimit;
            private readonly ConfigurableJointMotion xMotion, yMotion, zMotion;
            private readonly JointDrive xDrive, yDrive, zDrive;

            private bool owned, clamped;

            internal Travel(Transform target)
            {
                Body = target.GetComponent<Rigidbody>();
                var joints = target.GetComponents<Joint>();
                if (joints.Length == 0) return;
                if (joints.Length != 1 || Body == null || Body.isKinematic)
                {
                    Error = "A supported mechanism needs one joint and a dynamic Rigidbody.";
                    return;
                }

                hinge = joints[0] as HingeJoint;
                slider = joints[0] as ConfigurableJoint;
                connected = joints[0].connectedBody;
                anchor = joints[0].anchor;
                connectedAnchor = joints[0].connectedAnchor;
                automatic = joints[0].autoConfigureConnectedAnchor;

                Vector3 direction;
                if (hinge != null)
                {
                    kind = 1;
                    hingeLimits = hinge.limits;
                    minimum = hingeLimits.min;
                    maximum = hingeLimits.max;
                    motor = hinge.useMotor;
                    spring = hinge.useSpring;
                    limits = hinge.useLimits;
                    direction = hinge.axis.normalized;
                    if (!limits || maximum - minimum <= 0.001f || direction.sqrMagnitude < 0.5f)
                    {
                        Error = "Hinge limits must define a closed minimum and an open maximum.";
                        return;
                    }
                }
                else if (slider != null)
                {
                    kind = 2;
                    xMotion = slider.xMotion;
                    yMotion = slider.yMotion;
                    zMotion = slider.zMotion;
                    xDrive = slider.xDrive;
                    yDrive = slider.yDrive;
                    zDrive = slider.zDrive;
                    linearLimit = slider.linearLimit;
                    minimum = -linearLimit.limit;
                    maximum = linearLimit.limit;

                    var axes = new[] { xMotion, yMotion, zMotion };
                    var count = 0;
                    for (var i = 0; i < axes.Length; i++)
                    {
                        if (axes[i] == ConfigurableJointMotion.Limited)
                        {
                            axisIndex = i;
                            count++;
                        }
                        else if (axes[i] != ConfigurableJointMotion.Locked) count += 2;
                    }

                    var right = slider.axis.normalized;
                    var up = Vector3.ProjectOnPlane(slider.secondaryAxis, right).normalized;
                    if (count != 1 || maximum <= 0.00001f || right.sqrMagnitude < 0.5f || up.sqrMagnitude < 0.5f ||
                        slider.angularXMotion != ConfigurableJointMotion.Locked ||
                        slider.angularYMotion != ConfigurableJointMotion.Locked ||
                        slider.angularZMotion != ConfigurableJointMotion.Locked)
                    {
                        Error = "A slider needs one limited linear axis, two locked axes and locked rotation.";
                        return;
                    }

                    direction = axisIndex == 0 ? right
                        : axisIndex == 1 ? up
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
            private float Coordinate => kind == 1 ? hinge.angle : Vector3.Dot(Anchor - Reference, Axis);
            internal float Progress => Valid && Constrained ? Mathf.InverseLerp(minimum, maximum, Coordinate) : 0f;

            internal void Suspend()
            {
                if (!Valid || !Constrained) return;

                owned = true;
                if (hinge != null)
                {
                    hinge.useMotor = false;
                    hinge.useSpring = false;
                }
                else
                {
                    slider.xDrive = default;
                    slider.yDrive = default;
                    slider.zDrive = default;
                }
            }

            internal void Drive(float amount, float force, float damper, float maximumForce)
            {
                if (!Valid || !Constrained) return;

                Suspend();
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

            internal void Clamp(bool locked)
            {
                if (!Constrained || clamped == locked || (locked && !Valid)) return;

                Suspend();
                clamped = locked;
                if (hinge != null)
                {
                    var closed = hingeLimits;
                    closed.max = closed.min;
                    hinge.limits = locked ? closed : hingeLimits;
                    hinge.useLimits = locked || limits;
                }
                else if (slider != null && locked)
                {
                    slider.autoConfigureConnectedAnchor = false;
                    var world = Reference + Axis * minimum;
                    slider.connectedAnchor = connected != null ? connected.transform.InverseTransformPoint(world) : world;
                    slider.xMotion = slider.yMotion = slider.zMotion = ConfigurableJointMotion.Locked;
                }
                else if (slider != null)
                {
                    slider.autoConfigureConnectedAnchor = false;
                    slider.connectedAnchor = connectedAnchor;
                    slider.xMotion = xMotion;
                    slider.yMotion = yMotion;
                    slider.zMotion = zMotion;
                    slider.linearLimit = linearLimit;
                    slider.autoConfigureConnectedAnchor = automatic;
                }
            }

            internal void Restore()
            {
                if (!owned) return;

                Clamp(false);
                if (hinge != null)
                {
                    hinge.useMotor = motor;
                    hinge.useSpring = spring;
                    hinge.useLimits = limits;
                    hinge.limits = hingeLimits;
                }
                if (slider != null)
                {
                    slider.xDrive = xDrive;
                    slider.yDrive = yDrive;
                    slider.zDrive = zDrive;
                }
                owned = false;
            }
        }

        internal sealed class Observation
        {
            private Vector3 position;
            private Quaternion rotation;
            private float progress;
            private float since;
            private float lastStrength;
            private int direction, endpoint;

            internal void Seed(float value, Vector3 point, Quaternion orientation, bool bounded, float tolerance)
            {
                progress = value;
                position = point;
                rotation = orientation;
                direction = 0;
                since = lastStrength = 0f;
                endpoint = bounded ? (value <= tolerance ? -1 : value >= 1f - tolerance ? 1 : 0) : 0;
            }

            internal void Step(Prop owner, Context cause, float value, bool bounded, Vector3 point, Quaternion orientation,
                float delta, float interval, float reference, float tolerance)
            {
                var distance = Vector3.Distance(position, point);
                var angle = Quaternion.Angle(rotation, orientation) * Mathf.Deg2Rad;
                var change = value - progress;
                var moving = distance > 0.00001f || angle > 0.00001f || (bounded && Mathf.Abs(change) > 0.00001f);
                var strength = Mathf.Clamp01(Mathf.Max(distance, angle) / Mathf.Max(0.0001f, delta * reference));
                if (moving) lastStrength = strength;

                since += delta;
                if (moving && since >= interval)
                {
                    owner.Move(Result(owner, cause, strength));
                    since = 0f;
                }

                if (bounded)
                {
                    var next = Mathf.Abs(change) > 0.00001f ? (change > 0f ? 1 : -1) : 0;
                    if (next != 0 && next != direction)
                    {
                        if (next > 0) owner.Opening(Result(owner, cause, strength));
                        else owner.Closing(Result(owner, cause, strength));
                    }
                    direction = next;

                    if ((endpoint == -1 && value > tolerance * 2f) || (endpoint == 1 && value < 1f - tolerance * 2f))
                        endpoint = 0;

                    var arrival = value <= tolerance ? -1 : value >= 1f - tolerance ? 1 : 0;
                    if (arrival != 0 && arrival != endpoint)
                    {
                        endpoint = arrival;
                        var context = Result(owner, cause, moving ? strength : lastStrength);
                        context.Set(Limit, arrival < 0 ? Endpoint.Closed : Endpoint.Open);
                        if (arrival < 0) owner.Closed(context);
                        else owner.Opened(context);
                        owner.Limit(context);
                    }
                }

                progress = value;
                position = point;
                rotation = orientation;
            }

            internal static Context Result(Prop owner, Context cause, float strength)
            {
                var result = new Context(cause?.Source, owner);
                result.Set(Context.Strength, Mathf.Clamp01(strength));
                return result;
            }
        }

        private sealed class Shape
        {
            internal Collider Collider { get; }
            internal bool Valid { get; }
            internal float Radius { get; }
            internal float RotationRadius => kind == 1 ? 0f : extents.magnitude;
            internal float Thickness => Mathf.Min(extents.x, Mathf.Min(extents.y, extents.z)) * 2f;

            private readonly Vector3 offset;
            private readonly Vector3 center, extents;
            private readonly Vector3 capsuleAxis;
            private readonly Quaternion rotation;
            private readonly float radius, halfLength;
            private readonly int kind;

            internal Shape(Collider collider, Transform target)
            {
                Collider = collider;
                var matrix = collider.transform.localToWorldMatrix;
                var x = ((Vector3)matrix.GetColumn(0)).normalized;
                var y = ((Vector3)matrix.GetColumn(1)).normalized;
                var z = ((Vector3)matrix.GetColumn(2)).normalized;
                if (Mathf.Abs(Vector3.Dot(x, y)) > 0.0001f || Mathf.Abs(Vector3.Dot(x, z)) > 0.0001f ||
                    Mathf.Abs(Vector3.Dot(y, z)) > 0.0001f)
                    return;

                offset = Quaternion.Inverse(target.rotation) * (collider.transform.position - target.position);
                rotation = Quaternion.Inverse(target.rotation) * collider.transform.rotation;
                var scale = collider.transform.lossyScale;
                scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));

                if (collider is BoxCollider box)
                {
                    center = Quaternion.Inverse(collider.transform.rotation) *
                        (collider.transform.TransformPoint(box.center) - collider.transform.position);
                    extents = Vector3.Scale(box.size * 0.5f, scale);
                }
                else if (collider is SphereCollider sphere)
                {
                    kind = 1;
                    center = Quaternion.Inverse(collider.transform.rotation) *
                        (collider.transform.TransformPoint(sphere.center) - collider.transform.position);
                    radius = sphere.radius * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
                    extents = Vector3.one * radius;
                }
                else if (collider is CapsuleCollider capsule)
                {
                    kind = 2;
                    center = Quaternion.Inverse(collider.transform.rotation) *
                        (collider.transform.TransformPoint(capsule.center) - collider.transform.position);
                    var axis = capsule.direction;
                    capsuleAxis = axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;
                    radius = capsule.radius * Mathf.Max(scale[(axis + 1) % 3], scale[(axis + 2) % 3]);
                    halfLength = Mathf.Max(0f, capsule.height * scale[axis] * 0.5f - radius);
                    extents = Vector3.one * radius + capsuleAxis * halfLength;
                }
                else if (collider is MeshCollider mesh && mesh.convex && mesh.sharedMesh != null)
                {
                    center = Quaternion.Inverse(collider.transform.rotation) *
                        (collider.transform.TransformPoint(mesh.sharedMesh.bounds.center) - collider.transform.position);
                    extents = Vector3.Scale(mesh.sharedMesh.bounds.extents, scale);
                }
                else return;

                Radius = offset.magnitude + center.magnitude + extents.magnitude;
                Valid = extents.x > 0f && extents.y > 0f && extents.z > 0f;
            }

            internal Pose At(Pose pose) => new(pose.position + pose.rotation * offset, pose.rotation * rotation);

            internal Vector3 Center(Pose pose)
            {
                var shape = At(pose);
                return shape.position + shape.rotation * center;
            }

            internal int Overlap(Pose pose, float padding, Collider[] results)
            {
                var point = Center(pose);
                var orientation = At(pose).rotation;

                if (kind == 1)
                {
                    return Physics.OverlapSphereNonAlloc(point, radius + padding,
                        results, ~0, QueryTriggerInteraction.Ignore);
                }

                if (kind == 2)
                {
                    var axis = orientation * capsuleAxis * halfLength;
                    return Physics.OverlapCapsuleNonAlloc(point - axis, point + axis, radius + padding,
                        results, ~0, QueryTriggerInteraction.Ignore);
                }

                return Physics.OverlapBoxNonAlloc(point, extents + Vector3.one * padding,
                    results, orientation, ~0, QueryTriggerInteraction.Ignore);
            }

            internal int Sweep(Pose a, Pose b, float padding, RaycastHit[] results)
            {
                var point = Center(a);
                var shift = Center(b) - point;
                var distance = shift.magnitude;
                if (distance <= 0.0000001f) return 0;

                var direction = shift / distance;
                var orientation = At(a).rotation;

                if (kind == 1)
                {
                    return Physics.SphereCastNonAlloc(point, radius + padding,
                        direction, results, distance, ~0, QueryTriggerInteraction.Ignore);
                }

                if (kind == 2)
                {
                    var axis = orientation * capsuleAxis * halfLength;
                    return Physics.CapsuleCastNonAlloc(point - axis, point + axis, radius + padding,
                        direction, results, distance, ~0, QueryTriggerInteraction.Ignore);
                }

                return Physics.BoxCastNonAlloc(point, extents + Vector3.one * padding,
                    direction, results, orientation, distance, ~0, QueryTriggerInteraction.Ignore);
            }
        }

        #endregion

        public enum Endpoint { Closed, Open }
    }
}
