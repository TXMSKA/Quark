using System;
using UnityEngine;

namespace Quark
{
    [Serializable]
    public class TestEntityAddon : Addon<Entity>
    {
        public override void Hook(Entity owner)
        {
            base.Hook(owner);
            Debug.Log($"TestEntityAddon.Hook → {owner.name}");
            owner.OnFocus += LogFocus;
            owner.OnUnfocus += LogUnfocus;
        }

        public override void Unhook()
        {
            if (Owner == null) return;
            Owner.OnFocus -= LogFocus;
            Owner.OnUnfocus -= LogUnfocus;
            Debug.Log($"TestEntityAddon.Unhook ← {Owner.name}");
            base.Unhook();
        }

        void LogFocus(Context context) => Debug.Log($"TestEntityAddon: {Owner.name} Focus");
        void LogUnfocus(Context context) => Debug.Log($"TestEntityAddon: {Owner.name} Unfocus");
    }
}
