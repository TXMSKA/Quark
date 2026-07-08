using UnityEngine;

namespace Quark
{
    public class TestPropMod : Mod<Prop>
    {
        public override void Initialize(Prop owner)
        {
            base.Initialize(owner);
            Debug.Log($"TestPropMod.Initialize → {owner.name}");
        }

        public override void Teardown()
        {
            Debug.Log($"TestPropMod.Teardown ← {Owner.name}");
            base.Teardown();
        }
    }
}
