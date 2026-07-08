using System;
using UnityEngine;

namespace Quark
{
    [Serializable]
    public class TestServiceAddon : Addon<Service>
    {
        public override void Initialize(Service owner)
        {
            base.Initialize(owner);
            Debug.Log($"TestServiceAddon.Initialize → {owner.name}");
        }

        public override void Teardown()
        {
            Debug.Log($"TestServiceAddon.Teardown ← {Owner.name}");
            base.Teardown();
        }
    }
}
