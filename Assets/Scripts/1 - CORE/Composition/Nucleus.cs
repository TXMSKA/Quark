using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Quark
{
    [Serializable]
    public class Nucleus
    {
        [SerializeReference] private List<Addon> addons = new();

        private Values values;
        public Values Values => values ??= new();

        public TAddon Get<TAddon>() where TAddon : class => addons.OfType<TAddon>().FirstOrDefault();

        public virtual void Initialize(object owner)
        {
            foreach (var a in addons)
            {
                if (a == null) continue;
                a.Hook(owner);
            }
        }

        public virtual void Handle()
        {
            foreach (var a in addons)
                if (a != null && a.Enabled)
                    a.Handle();
        }

        public virtual void Teardown()
        {
            foreach (var a in addons)
                a?.Unhook();
        }
    }

    [Serializable]
    public sealed class Nucleus<T> : Nucleus where T : Component
    {
        private Mod<T>[] mods;

        public TMod Has<TMod>() where TMod : class => mods.OfType<TMod>().FirstOrDefault();

        public override void Initialize(object owner)
        {
            base.Initialize(owner);

            var host = (T)owner;
            mods = host.GetComponentsInChildren<Mod<T>>(true)
                .Where(m => m.GetComponentInParent<T>() == host)
                .ToArray();

            foreach (var m in mods)
                m.Hook(host);
        }

        public override void Handle()
        {
            if (mods == null)
                return;

            base.Handle();

            foreach (var m in mods)
                if (m != null && m.Enabled)
                    m.Handle();
        }

        public override void Teardown()
        {
            if (mods == null)
                return;

            base.Teardown();

            foreach (var m in mods)
                if (m != null)
                    m.Unhook();

            mods = null;
        }
    }
}
