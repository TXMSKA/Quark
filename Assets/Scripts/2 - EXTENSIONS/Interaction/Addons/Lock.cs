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
            compatible = true;
            Transform target = null;

            foreach (var candidate in owner.GetComponentsInChildren<Physical>(true))
            {
                if (candidate.GetComponentInParent<Prop>(true) == owner)
                {
                    if (target != null && target != candidate.transform) compatible = false;
                    target = candidate.transform;
                    physical = candidate;
                }
            }

            foreach (var candidate in owner.GetComponentsInChildren<Motion>(true))
            {
                if (candidate.GetComponentInParent<Prop>(true) == owner)
                {
                    if (target != null && target != candidate.Target(owner)) compatible = false;
                    target = candidate.Target(owner);
                    motion = candidate;
                }
            }

            pending = initiallyLocked;
            locked = false;
        }

        public override void Handle()
        {
            if (!pending) return;
            pending = false;
            Set(true);
        }

        public override void Unhook()
        {
            Set(false);
            pending = false;
            physical = null;
            motion = null;
            base.Unhook();
        }

        #endregion

        #region API

        public bool Locked
        {
            get
            {
                if (!Enabled) locked = false;
                return locked && Owner != null && Owner.isActiveAndEnabled;
            }
        }

        public bool Set(bool value)
        {
            pending = false;
            if (value)
            {
                if (!compatible || !Enabled || Owner == null || !Owner.isActiveAndEnabled) return false;
                if (Locked) return true;

                if (physical != null && physical.Bounded)
                {
                    if (physical.Progress > tolerance) return false;
                }
                else if (motion == null || !motion.Closed(tolerance)) return false;
            }

            locked = value;
            if (physical != null && physical.Active) physical.ApplyLock(value);
            else if (motion != null && motion.Active) motion.ApplyLock(value);
            return true;
        }

        #endregion

        #region MISC

        private Physical physical;
        private Motion motion;
        private bool locked;
        private bool pending;
        private bool compatible;

        #endregion
    }
}
