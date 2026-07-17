using UnityEngine;

namespace Quark
{
    public class TestServiceMod : Mod<Service>
    {
        public override void Hook(Service owner)
        {
            base.Hook(owner);
            Debug.Log($"TestServiceMod.Hook → {owner.name}");
        }

        public override void Unhook()
        {
            if (Owner == null) return;
            Debug.Log($"TestServiceMod.Unhook ← {Owner.name}");
            base.Unhook();
        }
    }
}
