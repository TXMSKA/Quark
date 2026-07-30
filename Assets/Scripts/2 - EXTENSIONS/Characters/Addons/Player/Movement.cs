using System;
using UnityEngine;

namespace Quark
{
    [Serializable]
    public class Movement : Addon<Player>
    {
        #region FIELDS

        public const string IsMoving = nameof(IsMoving);

        [SerializeField] private float speed = 3f;
        [SerializeField] private float walkSpeed = 2f;
        [SerializeField] private float acceleration = 20f;
        [SerializeField] private float deceleration = 25f;
        [SerializeField] private Nucleus nucleus = new();

        #endregion

        internal Controls controls;
        internal float velocityY;
        private bool walking;
        private Vector3 planar;
        private Crouch crouch;
        private Sprint sprint;
        private Jump jump;

        public override void Hook(Player owner)
        {
            base.Hook(owner);
            controls = GameManager.Find<Controls>();
            controls?.Subscribe(Control.Walk, Walk);
            nucleus.Initialize(this);
            crouch = nucleus.Get<Crouch>();
            sprint = nucleus.Get<Sprint>();
            jump = nucleus.Get<Jump>();
        }

        public override void Handle()
        {
            var axis = controls?.Axis(Control.Move) ?? default;
            var moving = axis.sqrMagnitude > 0.01f;
            Owner.Values.Set(IsMoving, moving);

            nucleus.Handle();

            var dir = Owner.transform.right * axis.x + Owner.transform.forward * axis.y;
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            var grounded = Owner.Controller.isGrounded;
            if (grounded && velocityY < 0f) velocityY = -2f;
            else velocityY += Physics.gravity.y * Time.deltaTime;

            Owner.Values.TryGet(Crouch.IsCrouching, out bool crouching);
            Owner.Values.TryGet(Sprint.IsSprinting, out bool sprinting);
            var cap = crouching && crouch != null ? crouch.Speed : sprinting && sprint != null ? sprint.Speed : walking ? walkSpeed : speed;

            var target = dir * cap;
            var rate = moving ? acceleration : deceleration;
            if (!grounded && jump != null) rate *= jump.AirControl;
            planar = Vector3.MoveTowards(planar, target, rate * Time.deltaTime);

            Owner.Controller.Move((planar + Vector3.up * velocityY) * Time.deltaTime);
        }

        private void Walk() => walking = !walking;

        public override void Unhook()
        {
            nucleus.Teardown();
            controls?.Unsubscribe(Control.Walk, Walk);
            controls = null;
            walking = false;
            velocityY = 0f;
            planar = default;
            crouch = null;
            sprint = null;
            jump = null;
            base.Unhook();
        }
    }
}
