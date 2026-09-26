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

        public void Cast(Ray ray, Host source = null) => Cast(ray, distance, source);
        public void Cast(Vector3 from, Vector3 to, Host source = null) =>
            Cast(new Ray(from, to - from), Vector3.Distance(from, to), source);

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

        private void Cast(Ray ray, float range, Host source)
        {
            var found = Physics.Raycast(ray, out var hit, range, layer)
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
            focus.Set(Context.Point, hit.point);
            focus.Set(Context.Normal, hit.normal);
            focus.Set(Context.View, ray);
            focus.Set(Context.SampleTime, Time.time);
            if (changed) Target.Focus(focus);
        }

        #endregion
    }
}
