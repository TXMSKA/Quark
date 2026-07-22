using System;
using UnityEngine;

namespace Quark
{
    [Serializable]
    public class Sprint : Addon<Movement>
    {
        #region FIELDS

        [SerializeField] private float speed = 6f;
        [SerializeField] private float drain = 3f;

        #endregion

        private bool held;
        private Stats.Stat stamina;

        public float Speed => speed;

        public override void Hook(Movement owner)
        {
            base.Hook(owner);
            owner.controls?.Subscribe(Control.Sprint, Press, Release);
            var stats = owner.Owner.Get<Stats>();
            stats?.TryGet(Stats.Stat.Kind.Stamina, out stamina);
        }

        public override void Handle()
        {
            Owner.Owner.Values.TryGet("IsMoving", out bool moving);
            Owner.Owner.Values.TryGet("IsCrouching", out bool crouching);
            var active = held && moving && !crouching && (stamina == null || stamina.Value > 0f);
            Owner.Owner.Values.Set("IsSprinting", active);
            if (active && stamina != null) stamina.Value -= drain * Time.deltaTime;
        }

        public override void Unhook()
        {
            Owner.controls?.Unsubscribe(Control.Sprint, Press, Release);
            Owner.Owner.Values.Set("IsSprinting", false);
            held = false;
            stamina = null;
            base.Unhook();
        }

        private void Press() => held = Enabled;
        private void Release() => held = false;
    }
}
