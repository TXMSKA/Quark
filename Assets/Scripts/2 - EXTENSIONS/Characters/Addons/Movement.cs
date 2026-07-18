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
        // Checkear si no hay forma de compactar las variables

        public override void Hook(Player owner)
        {
            base.Hook(owner);
            if (GameManager.Instance != null && GameManager.Instance.Values.TryGet("Controls", out Controls controls))
                move = controls.Get(Control.Move);
        }

        // Comprobar cómo va a funcionar esto bien.
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
        public class Jump : Addon<Movement>
        {
            // Hook y Unhook para keys y eventos.
            // Debería nomás detectar el salto posible y agregar speed Y.
        }

        [Serializable]
        public class Crouch : Addon<Movement>
        {
            #region FIELDS

            [SerializeField, Min(0f)] private float timeToCrouch = 0.133f;

            [Header("Standing")]
            [SerializeField] private Vector3 standingCenter = new(0f, 0.95f, 0f);
            [SerializeField, Min(0.01f)] private float standingHeight = 1.7f;
            [SerializeField, Min(0.01f)] private float standingRadius = 0.25f;

            [Header("Crouching")]
            [SerializeField] private Vector3 crouchingCenter = new(0f, 0.7f, 0f);
            [SerializeField, Min(0.01f)] private float crouchHeight = 1.2f;
            [SerializeField, Min(0.01f)] private float crouchRadius = 0.5f;

            #endregion

            // Hook y Unhook para las keys y los eventos.
            // O bien Handle o una Coroutine que maneja el target del Crouch.
        }

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
