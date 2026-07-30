using System;
using UnityEngine;

namespace Quark
{
    [Serializable]
    public class Sprint : Addon<Movement>
    {
        #region FIELDS

        public const string IsSprinting = nameof(IsSprinting);

        [SerializeField] private float speed = 6f;
        [SerializeField] private float drain = 3f;

        #endregion

        private bool held, toggled;
        private Stats.Stat stamina;

        public float Speed => speed;

        public override void Hook(Movement owner)
        {
            base.Hook(owner);
            owner.controls?.Subscribe(Control.Sprint, Press, Release);
            owner.controls?.Subscribe(Control.SprintToggle, Toggle);
            var stats = owner.Owner.Get<Stats>();
            stats?.TryGet(Stats.Stat.Kind.Stamina, out stamina);
        }

        public override void Handle()
        {
            Owner.Owner.Values.TryGet(Movement.IsMoving, out bool moving);
            Owner.Owner.Values.TryGet(Crouch.IsCrouching, out bool crouching);
            if (!moving) toggled = false;
            var active = (held || toggled) && moving && !crouching && (stamina == null || stamina.Value > 0f);
            Owner.Owner.Values.Set(IsSprinting, active);
            if (active && stamina != null) stamina.Value -= drain * Time.deltaTime;
        }

        public override void Unhook()
        {
            Owner.controls?.Unsubscribe(Control.Sprint, Press, Release);
            Owner.controls?.Unsubscribe(Control.SprintToggle, Toggle);
            Owner.Owner.Values.Set(IsSprinting, false);
            held = toggled = false;
            stamina = null;
            base.Unhook();
        }

        private void Press() => held = Enabled;
        private void Release() => held = false;
        private void Toggle() { if (Enabled) toggled = !toggled; }
    }
}
