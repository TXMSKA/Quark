using System;
using UnityEngine;

namespace Quark
{
    [Serializable]
    public class Lock : Addon<Prop>
    {
        #region FIELDS

        [SerializeField] private bool initiallyLocked;
        [SerializeField, Range(0.001f, 0.1f)] private float tolerance = 0.01f;

        #endregion

        #region LIFETIME

        public override void Hook(Prop owner)
        {
            base.Hook(owner);
            pending = initiallyLocked;
        }

        // The first Handle runs after the owner's mods have hooked, which the closed check needs.
        public override void Handle()
        {
            if (pending) Set(true);
        }

        public override void Unhook()
        {
            Set(false);
            base.Unhook();
        }

        #endregion

        #region API

        public bool Locked { get; private set; }

        public bool Set(bool value)
        {
            pending = false;
            if (value == Locked) return true;

            if (value)
            {
                if (!Enabled || Owner == null || !Owner.isActiveAndEnabled) return false;
                var physical = Owner.Get<Physical>();
                var motion = Owner.Get<Motion>();
                var mechanism = physical != null && physical.Active ? physical.Mechanism
                    : motion != null && motion.Active ? motion.Mechanism
                    : null;
                var closed = mechanism != null &&
                    (mechanism.Constrained ? mechanism.Progress <= tolerance : motion != null && motion.Closed(tolerance));
                if (!closed) return false;

                if (motion != null) motion.Stop();
                if (mechanism.Constrained)
                {
                    frozen = mechanism.Body;
                    frozen.isKinematic = true;
                }
            }
            else if (frozen != null)
            {
                frozen.isKinematic = false;
                frozen = null;
            }

            Locked = value;
            return true;
        }

        #endregion

        #region MISC

        private Rigidbody frozen;
        private bool pending;

        #endregion
    }
}
