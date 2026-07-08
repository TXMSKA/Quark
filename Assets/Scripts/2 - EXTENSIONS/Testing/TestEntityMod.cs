using UnityEngine;

namespace Quark
{
    public class TestEntityMod : Mod<Entity>
    {
        public override void Initialize(Entity owner)
        {
            base.Initialize(owner);
            Debug.Log($"TestEntityMod.Initialize → {owner.name}");
        }

        public override void Teardown()
        {
            Debug.Log($"TestEntityMod.Teardown ← {Owner.name}");
            base.Teardown();
        }
    }
}
