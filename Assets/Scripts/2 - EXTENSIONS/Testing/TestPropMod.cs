using UnityEngine;

namespace Quark
{
    public class TestPropMod : Mod<Prop>
    {
        public override void Hook(Prop owner)
        {
            base.Hook(owner);
            Debug.Log($"TestPropMod.Hook → {owner.name}");
        }

        public override void Unhook()
        {
            if (Owner == null) return;
            Debug.Log($"TestPropMod.Unhook ← {Owner.name}");
            base.Unhook();
        }
    }
}
