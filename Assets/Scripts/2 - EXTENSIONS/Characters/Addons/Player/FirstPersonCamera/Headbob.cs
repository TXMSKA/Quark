using System;
using UnityEngine;

namespace Quark
{
    [Serializable]
    public class Headbob : Addon<FirstPersonCamera>
    {
        #region FIELDS

        [SerializeField] private float frequency = 6f;
        [SerializeField] private Vector2 amplitude = new(0.025f, 0.035f);
        [SerializeField, Min(0.01f)] private float speedReference = 3f;
        [SerializeField] private float returnSpeed = 12f;

        #endregion

        private Vector3 offset;
        private float phase;

        public float Phase => phase;

        public override void Handle()
        {
            var root = Owner.Root;
            if (root == null) return;

            var velocity = Owner.Owner.Controller.velocity;
            velocity.y = 0f;
            var scale = Owner.Owner.Controller.isGrounded ? velocity.magnitude / speedReference : 0f;
            if (scale > 0.01f) phase += frequency * scale * Time.deltaTime;

            var target = new Vector3(Mathf.Sin(phase) * amplitude.x, -Mathf.Abs(Mathf.Sin(phase)) * amplitude.y, 0f) * scale;
            offset = Vector3.Lerp(offset, target, returnSpeed * Time.deltaTime);
            root.localPosition += offset;
        }

        public override void Unhook()
        {
            offset = default;
            phase = 0f;
            base.Unhook();
        }
    }
}
