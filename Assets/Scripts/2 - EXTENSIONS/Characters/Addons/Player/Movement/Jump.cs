using System;
using UnityEngine;

namespace Quark
{
    [Serializable]
    public class Jump : Addon<Movement>
    {
        #region FIELDS

        [SerializeField] private float force = 5f;
        [SerializeField, Range(0f, 1f)] private float airControl = 1f;

        #endregion

        public override void Hook(Movement owner)
        {
            base.Hook(owner);
            owner.controls?.Subscribe(Control.Jump, Perform);
        }

        public float AirControl => airControl;

        public override void Unhook()
        {
            Owner.controls?.Unsubscribe(Control.Jump, Perform);
            base.Unhook();
        }

        private void Perform()
        {
            if (!Enabled) return;
            Owner.Owner.Values.TryGet(Crouch.IsCrouching, out bool crouching);
            if (Owner.Owner.Controller.isGrounded && !crouching) Owner.velocityY = force;
        }
    }
}
