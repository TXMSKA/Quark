using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Quark
{
    [Serializable]
    public sealed class Nucleus<T> where T : Component
    {
        [SerializeReference] List<Addon<T>> addons = new();
        Mod<T>[] mods;

        public bool Initialized => mods != null;

        public void Initialize(T owner)
        {
            addons.ForEach(a => a?.Initialize(owner));
            mods = owner.GetComponentsInChildren<Mod<T>>(true).Where(m => m.GetComponentInParent<T>() == owner).ToArray();
            foreach (var m in mods) m.Initialize(owner);
        }

        public void Teardown()
        {
            if (mods == null) return;
            foreach (var m in mods) if (m != null) m.Teardown();
            addons.ForEach(a => a?.Teardown());
        }
    }
}
