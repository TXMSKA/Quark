using System;
using System.Collections.Generic;
using UnityEngine;

namespace Quark
{
    [Serializable]
    public class Movement : Addon<Player>
    {
        #region FIELDS

        [SerializeField] private float speed = 3f;
        [SerializeField] private float acceleration = 20f;
        [SerializeField] private float deceleration = 25f;
        [SerializeReference] private List<Addon> addons = new();

        #endregion

        internal Controls controls;
        internal float velocityY;
        private Vector3 planar;
        private Crouch crouch;
        private Sprint sprint;
        private Jump jump;

        public override void Hook(Player owner)
        {
            base.Hook(owner);
            controls = GameManager.Find<Controls>();
            foreach (var addon in addons)
            {
                addon?.Hook(this);
                switch (addon)
                {
                    case Crouch c:
                        crouch = c;
                        break;
                    case Sprint s:
                        sprint = s;
                        break;
                    case Jump j:
                        jump = j;
                        break;
                }
            }
        }

        public override void Handle()
        {
            var axis = controls?.Axis(Control.Move) ?? default;
            var moving = axis.sqrMagnitude > 0.01f;
            Owner.Values.Set("IsMoving", moving);

            foreach (var addon in addons)
                if (addon != null && addon.Enabled)
                    addon.Handle();

            var dir = Owner.transform.right * axis.x + Owner.transform.forward * axis.y;
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            var grounded = Owner.Controller.isGrounded;
            if (grounded && velocityY < 0f) velocityY = -2f;
            else velocityY += Physics.gravity.y * Time.deltaTime;

            Owner.Values.TryGet("IsCrouching", out bool crouching);
            Owner.Values.TryGet("IsSprinting", out bool sprinting);
            var cap = crouching && crouch != null ? crouch.Speed : sprinting && sprint != null ? sprint.Speed : speed;

            var target = dir * cap;
            var rate = moving ? acceleration : deceleration;
            if (!grounded && jump != null) rate *= jump.AirControl;
            planar = Vector3.MoveTowards(planar, target, rate * Time.deltaTime);

            Owner.Controller.Move((planar + Vector3.up * velocityY) * Time.deltaTime);
        }

        public override void Unhook()
        {
            foreach (var addon in addons) addon?.Unhook();
            controls = null;
            velocityY = 0f;
            planar = default;
            crouch = null;
            sprint = null;
            jump = null;
            base.Unhook();
        }
    }
}
