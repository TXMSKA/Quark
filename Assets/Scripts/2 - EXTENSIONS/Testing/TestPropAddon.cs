using System;
using UnityEngine;

namespace Quark
{
    [Serializable]
    public class TestPropAddon : Addon<Prop>
    {
        public override void Initialize(Prop owner)
        {
            base.Initialize(owner);
            Debug.Log($"TestPropAddon.Initialize → {owner.name}");
        }

        public override void Teardown()
        {
            Debug.Log($"TestPropAddon.Teardown ← {Owner.name}");
            base.Teardown();
        }
    }
}
