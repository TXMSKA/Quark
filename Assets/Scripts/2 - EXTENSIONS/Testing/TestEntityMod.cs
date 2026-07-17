using UnityEngine;

namespace Quark
{
    public class TestEntityMod : Mod<Entity>
    {
        public override void Hook(Entity owner)
        {
            base.Hook(owner);
            Debug.Log($"TestEntityMod.Hook → {owner.name}");
        }

        public override void Unhook()
        {
            if (Owner == null) return;
            Debug.Log($"TestEntityMod.Unhook ← {Owner.name}");
            base.Unhook();
        }
    }
}
