using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Quark
{
    [Serializable]
    public sealed class Nucleus<T> where T : Component
    {
        [SerializeReference] private List<Addon> addons = new();
        private Mod<T>[] mods;

        private Values values;
        public Values Values => values ??= new();

        public TAddon Get<TAddon>() where TAddon : class => addons.OfType<TAddon>().FirstOrDefault();
        public TMod Has<TMod>() where TMod : class => mods.OfType<TMod>().FirstOrDefault();

        public void Initialize(T owner)
        {
            foreach (var a in addons)
            {
                if (a == null) continue;
                a.Hook(owner);
            }
            mods = owner.GetComponentsInChildren<Mod<T>>(true).Where(m => m.GetComponentInParent<T>() == owner).ToArray();
            foreach (var m in mods) m.Hook(owner);
        }

        public void Handle()
        {
            if (mods == null) return;
            foreach (var a in addons) a?.Handle();
            foreach (var m in mods) if (m != null) m.Handle();
        }

        public void Teardown()
        {
            if (mods == null) return;
            foreach (var a in addons) a?.Unhook();
            foreach (var m in mods) if (m != null) m.Unhook();
            mods = null;
        }
    }
}
