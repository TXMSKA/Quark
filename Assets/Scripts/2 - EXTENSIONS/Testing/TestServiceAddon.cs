using System;
using UnityEngine;

namespace Quark
{
    [Serializable]
    public class TestServiceAddon : Addon<Service>
    {
        public override void Hook(Service owner)
        {
            base.Hook(owner);
            Debug.Log($"TestServiceAddon.Hook → {owner.name}");
        }

        public override void Unhook()
        {
            if (Owner == null) return;
            Debug.Log($"TestServiceAddon.Unhook ← {Owner.name}");
            base.Unhook();
        }
    }
}
