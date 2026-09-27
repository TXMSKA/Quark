using System;
using UnityEngine;

namespace Quark
{
    [Serializable]
    public class Caster
    {
        #region FIELDS

        [SerializeField, Min(0f)] private float distance = 3f;
        [SerializeField] private LayerMask layer = ~0;

        #endregion

        #region API

        public Identifiable Target { get; private set; }
        public RaycastHit Hit { get; private set; }

        public void Cast(Ray ray, Host source = null)
        {
            var found = Physics.Raycast(ray, out var hit, distance, layer)
                ? hit.collider.GetComponentInParent<Identifiable>()
                : null;
            if (found == null) { Clear(); return; }

            var changed = found != Target || focus == null || !ReferenceEquals(focus.Source, source);
            if (changed)
            {
                Clear();
                Target = found;
                focus = new Context(source, found);
            }

            Hit = hit;
            if (changed) Target.Focus(focus);
        }

        public void Clear()
        {
            var previous = Target;
            var context = focus;
            Target = null;
            Hit = default;
            focus = null;
            if (previous != null) previous.Unfocus(context);
        }

        #endregion

        #region MISC

        private Context focus;

        #endregion
    }
}
