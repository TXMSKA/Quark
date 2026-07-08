using UnityEngine;

namespace Quark
{
    public class TestServiceMod : Mod<Service>
    {
        public override void Initialize(Service owner)
        {
            base.Initialize(owner);
            Debug.Log($"TestServiceMod.Initialize → {owner.name}");
        }

        public override void Teardown()
        {
            Debug.Log($"TestServiceMod.Teardown ← {Owner.name}");
            base.Teardown();
        }
    }
}
