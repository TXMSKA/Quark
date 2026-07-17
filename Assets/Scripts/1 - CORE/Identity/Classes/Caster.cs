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

        public void Cast(Ray ray) => Cast(ray, distance);
        public void Cast(Vector3 from, Vector3 to) => Cast(new Ray(from, to - from), Vector3.Distance(from, to));

        public void Clear()
        {
            if (Target != null) Target.Unfocus(new Context());
            Target = null;
        }

        #endregion

        #region MISC

        private void Cast(Ray ray, float range)
        {
            Hit = Physics.Raycast(ray, out var hit, range, layer) ? hit : default;
            var found = Hit.collider != null ? Hit.collider.GetComponentInParent<Identifiable>() : null;
            if (found == Target) return;
            if (Target != null) Target.Unfocus(new Context());
            Target = found;
            if (Target != null) Target.Focus(new Context());
        }

        #endregion
    }
}
