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

        public override void Hook(FirstPersonCamera owner)
        {
            base.Hook(owner);
            owner.controls?.Subscribe(Control.Interact, Perform);
        }

        public override void Handle()
        {
            var root = Owner.Root;
            if (root != null) caster.Cast(new Ray(root.position, root.forward));
        }

        public override void Unhook()
        {
            Owner.controls?.Unsubscribe(Control.Interact, Perform);
            caster.Clear();
            base.Unhook();
        }

        private void Perform()
        {
            if (Enabled && caster.Target is Prop prop) prop.Interact(new Context());
        }
    }
}
