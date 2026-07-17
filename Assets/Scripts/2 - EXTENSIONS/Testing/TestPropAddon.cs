using System;
using UnityEngine;

namespace Quark
{
    [Serializable]
    public class TestPropAddon : Addon<Prop>
    {
        public override void Hook(Prop owner)
        {
            base.Hook(owner);
            Debug.Log($"TestPropAddon.Hook → {owner.name}");
            owner.OnFocus += LogFocus;
            owner.OnUnfocus += LogUnfocus;
            owner.OnUse += LogUse;
            owner.OnInteract += LogInteract;
        }

        public override void Unhook()
        {
            if (Owner == null) return;
            Owner.OnFocus -= LogFocus;
            Owner.OnUnfocus -= LogUnfocus;
            Owner.OnUse -= LogUse;
            Owner.OnInteract -= LogInteract;
            Debug.Log($"TestPropAddon.Unhook ← {Owner.name}");
            base.Unhook();
        }

        void LogFocus(Context context) => Debug.Log($"TestPropAddon: {Owner.name} Focus");
        void LogUnfocus(Context context) => Debug.Log($"TestPropAddon: {Owner.name} Unfocus");
        void LogUse(Context context) => Debug.Log($"TestPropAddon: {Owner.name} Use");
        void LogInteract(Context context) => Debug.Log($"TestPropAddon: {Owner.name} Interact");
    }
}
