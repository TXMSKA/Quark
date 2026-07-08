using System;
using UnityEngine;

namespace Quark
{
    [Serializable]
    public class TestEntityAddon : Addon<Entity>
    {
        public override void Initialize(Entity owner)
        {
            base.Initialize(owner);
            Debug.Log($"TestEntityAddon.Initialize → {owner.name}");
        }

        public override void Teardown()
        {
            Debug.Log($"TestEntityAddon.Teardown ← {Owner.name}");
            base.Teardown();
        }
    }
}
