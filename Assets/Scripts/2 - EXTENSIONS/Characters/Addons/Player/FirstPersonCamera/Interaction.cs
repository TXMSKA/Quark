using System;
using UnityEngine;

namespace Quark
{
    [Serializable]
    public class Interaction : Addon<FirstPersonCamera>
    {
        #region FIELDS

        [SerializeField] private Caster caster = new();

        #endregion

        #region LIFETIME

        public override void Hook(FirstPersonCamera owner)
        {
            base.Hook(owner);
            owner.controls?.Subscribe(Control.Use, Use, ReleaseUse);
            owner.controls?.Subscribe(Control.Interact, Primary, ReleasePrimary);
            owner.controls?.Subscribe(Control.Secondary, Secondary, ReleaseSecondary);
        }

        public override void Handle()
        {
            if (!Ready) { Cancel(); return; }
            Cast();
            Sample(primary);
            Sample(secondary);
            Sample(use);
        }

        public override void Unhook()
        {
            Cancel();
            if (Owner != null)
            {
                Owner.controls?.Unsubscribe(Control.Use, Use, ReleaseUse);
                Owner.controls?.Unsubscribe(Control.Interact, Primary, ReleasePrimary);
                Owner.controls?.Unsubscribe(Control.Secondary, Secondary, ReleaseSecondary);
            }
            base.Unhook();
        }

        #endregion

        #region MISC

        private Context primary, secondary, use;
        private Prop tracked, grabbed;

        private bool Ready => Enabled && Owner != null && Owner.Enabled && Owner.Owner != null &&
            Owner.Owner.isActiveAndEnabled && Owner.Root != null && Owner.Root.gameObject.activeInHierarchy;

        private void Cast() => caster.Cast(new Ray(Owner.Root.position, Owner.Root.forward), Owner.Owner);
        private void Primary() => Press(ref primary, Context.Channel.Primary);
        private void Secondary() => Press(ref secondary, Context.Channel.Secondary);
        private void Use() => Press(ref use, null);
        private void ReleaseSecondary() => Release(ref secondary, false);
        private void ReleaseUse() => Release(ref use, true);

        private void ReleasePrimary()
        {
            Release(ref primary, false);
            Untrack();
        }

        private void Press(ref Context operation, Context.Channel? channel)
        {
            if (!Ready || operation != null) return;

            Cast();
            var target = grabbed != null && grabbed.isActiveAndEnabled ? grabbed : caster.Target as Prop;
            if (target == null || !target.isActiveAndEnabled) return;

            operation = new Context(Owner.Owner, target);
            operation.Set(Context.Stage, Context.Phase.Press);
            if (channel.HasValue) operation.Set(Context.Input, channel.Value);
            if (caster.Target == target && caster.Hit.collider != null) operation.Set(Context.Point, caster.Hit.point);
            Sample(operation);

            if (channel == Context.Channel.Primary)
            {
                Untrack();
                tracked = target;
                tracked.OnGrab += Grabbed;
                tracked.OnRelease += Released;
            }

            if (channel.HasValue) target.Interact(operation);
            else target.Use(operation);
        }

        private void Release(ref Context operation, bool isUse)
        {
            var context = operation;
            operation = null;
            if (context == null) return;

            context.Set(Context.Stage, Context.Phase.Release);
            Sample(context);
            if (context.Target is not Prop target || target == null) return;

            if (isUse) target.Use(context);
            else target.Interact(context);
        }

        private void Sample(Context context)
        {
            if (context == null || Owner == null || Owner.Owner == null || Owner.Root == null) return;

            var player = Owner.Owner;
            context.Set(Context.View, new Ray(Owner.Root.position, Owner.Root.forward));
            context.Set(Context.SampleTime, Time.time);
            context.Set(Context.Velocity, player.Controller != null ? player.Controller.velocity : Vector3.zero);
            player.Values.TryGet(Crouch.IsCrouching, out bool stealth);
            context.Set(Context.Stealth, stealth);
        }

        private void Grabbed(Context context)
        {
            if (context != null && primary != null && context.Source == primary.Source && context.Target == tracked)
                grabbed = tracked;
        }

        private void Released(Context context)
        {
            if (context != null && primary != null && context.Source == primary.Source && context.Target == grabbed)
                grabbed = null;
        }

        private void Untrack()
        {
            if (tracked != null)
            {
                tracked.OnGrab -= Grabbed;
                tracked.OnRelease -= Released;
            }
            tracked = grabbed = null;
        }

        private void Cancel()
        {
            ReleaseSecondary();
            ReleaseUse();
            ReleasePrimary();
            caster.Clear();
        }

        #endregion
    }
}
