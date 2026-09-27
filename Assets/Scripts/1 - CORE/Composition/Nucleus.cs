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

        public virtual TBehavior Get<TBehavior>() where TBehavior : class =>
            addons.OfType<TBehavior>().FirstOrDefault();

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

        // Mods hook after addons, so an addon asking during its own Hook finds only addons.
        public override TBehavior Get<TBehavior>() =>
            base.Get<TBehavior>() ?? mods?.OfType<TBehavior>().FirstOrDefault();

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
                if (m != null && m.isActiveAndEnabled)
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
