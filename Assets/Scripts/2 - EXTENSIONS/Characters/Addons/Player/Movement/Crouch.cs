using System;
using UnityEngine;

namespace Quark
{
    [Serializable]
    public class Crouch : Addon<Movement>
    {
        #region FIELDS

        [SerializeField] private float speed = 1.5f;
        [SerializeField, Min(0f)] private float timeToCrouch = 0.133f;

        [Header("Standing")]
        [SerializeField] private Vector3 standingCenter = new(0f, 0.95f, 0f);
        [SerializeField, Min(0.01f)] private float standingHeight = 1.7f;
        [SerializeField, Min(0.01f)] private float standingRadius = 0.25f;

        [Header("Crouching")]
        [SerializeField] private Vector3 crouchingCenter = new(0f, 0.7f, 0f);
        [SerializeField, Min(0.01f)] private float crouchHeight = 1.2f;
        [SerializeField, Min(0.01f)] private float crouchRadius = 0.5f;

        // Temporal: move to global Settings (world/interaction layers).
        [SerializeField] private LayerMask blockers = ~0;

        #endregion

        private bool toggled, held;
        private float amount;

        public float Speed => speed;

        public override void Hook(Movement owner)
        {
            base.Hook(owner);
            owner.controls?.Subscribe(Control.Crouch, Toggle);
            owner.controls?.Subscribe(Control.CrouchHold, Hold, Release);
        }

        public override void Handle()
        {
            var target = toggled || held || (amount > 0f && Blocked);
            Owner.Owner.Values.Set("IsCrouching", target);
            if (!target && amount == 0f) return;

            amount = Mathf.MoveTowards(amount, target ? 1f : 0f, Time.deltaTime / timeToCrouch);
            var controller = Owner.Owner.Controller;
            controller.height = Mathf.Lerp(standingHeight, crouchHeight, amount);
            controller.center = Vector3.Lerp(standingCenter, crouchingCenter, amount);
            controller.radius = Mathf.Lerp(standingRadius, crouchRadius, amount);
        }

        public override void Unhook()
        {
            Owner.controls?.Unsubscribe(Control.Crouch, Toggle);
            Owner.controls?.Unsubscribe(Control.CrouchHold, Hold, Release);
            Owner.Owner.Values.Set("IsCrouching", false);
            toggled = held = false;
            amount = 0f;
            base.Unhook();
        }

        private void Toggle() { if (Enabled) toggled = !toggled; }
        private void Hold() => held = Enabled;
        private void Release() => held = false;

        private bool Blocked
        {
            get
            {
                var controller = Owner.Owner.Controller;
                return Physics.Raycast(controller.transform.position + Vector3.up * controller.height, Vector3.up, standingHeight - controller.height, blockers, QueryTriggerInteraction.Ignore);
            }
        }
    }
}
