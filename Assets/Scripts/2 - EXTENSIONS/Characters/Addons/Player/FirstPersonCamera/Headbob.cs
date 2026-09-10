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

        [Header("Jump")]
        [SerializeField] private bool jolt = true;
        [SerializeField, ShowIf(nameof(jolt)), Min(0f)] private float jumpStrength = 0.05f;
        [SerializeField, ShowIf(nameof(jolt)), Min(0f)] private float landStrength = 0.12f;
        [SerializeField, ShowIf(nameof(jolt)), Min(0.01f)] private float landReference = 8f;

        #endregion

        private Vector3 offset;
        private float phase;
        private float kick;
        private float fall;
        private bool grounded = true;

        public float Phase => phase;

        public override void Handle()
        {
            var root = Owner.Root;
            if (root == null) return;

            var controller = Owner.Owner.Controller;
            var velocity = controller.velocity;
            var planar = velocity;
            planar.y = 0f;

            var scale = controller.isGrounded ? planar.magnitude / speedReference : 0f;
            if (scale > 0.01f) phase += frequency * scale * Time.deltaTime;

            var target = new Vector3(Mathf.Sin(phase) * amplitude.x, -Mathf.Abs(Mathf.Sin(phase)) * amplitude.y, 0f) * scale;
            offset = Vector3.Lerp(offset, target, returnSpeed * Time.deltaTime);

            if (jolt)
            {
                if (!controller.isGrounded) fall = Mathf.Max(fall, -velocity.y);
                if (controller.isGrounded != grounded)
                {
                    kick += controller.isGrounded ? -landStrength * Mathf.Clamp01(fall / landReference) : jumpStrength;
                    grounded = controller.isGrounded;
                    if (grounded) fall = 0f;
                }
                kick = Mathf.Lerp(kick, 0f, returnSpeed * Time.deltaTime);
                offset.y += kick;
            }

            root.localPosition += offset;
        }

        public override void Unhook()
        {
            offset = default;
            phase = 0f;
            kick = 0f;
            fall = 0f;
            grounded = true;
            base.Unhook();
        }
    }
}
