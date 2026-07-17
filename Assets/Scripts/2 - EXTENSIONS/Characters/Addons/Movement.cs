using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Quark
{
    [Serializable]
    public class Movement : Addon<Player>
    {
        #region FIELDS

        [SerializeField] private float speed = 3f;

        #endregion

        private InputAction move;
        private Vector2 axis;
        private float velocityY;

        public override void Hook(Player owner)
        {
            base.Hook(owner);
            if (GameManager.Instance != null && GameManager.Instance.Values.TryGet("Controls", out Controls controls))
                move = controls.Get(Control.Move);
        }

        public override void Handle()
        {
            axis = move?.ReadValue<Vector2>() ?? default;
            var dir = Owner.transform.right * axis.x + Owner.transform.forward * axis.y;
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            if (Owner.Controller.isGrounded && velocityY < 0f) velocityY = -2f;
            else velocityY += Physics.gravity.y * Time.deltaTime;

            Owner.Controller.Move((dir * speed + Vector3.up * velocityY) * Time.deltaTime);
        }

        public override void Unhook()
        {
            move = null;
            velocityY = 0f;
            base.Unhook();
        }

        [Serializable]
        public class Jump : Addon<Movement> { }

        [Serializable]
        public class Crouch : Addon<Movement> { }

        [Serializable]
        public class Sprint : Addon<Movement>
        {
            #region FIELDS

            [SerializeField] private float drain = 3f;

            #endregion

            private Stats.Stat stamina;

            public override void Hook(Movement owner)
            {
                base.Hook(owner);
                var stats = owner.Owner.Get<Stats>();
                stats?.TryGet(Stats.Stat.Kind.Stamina, out stamina);
            }

            public override void Handle()
            {
                if (stamina == null) return;
                stamina.Value -= drain * Time.deltaTime;
            }

            public override void Unhook()
            {
                stamina = null;
                base.Unhook();
            }
        }
    }
}
