using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Quark
{
    [Serializable]
    public class FirstPersonCamera : Addon<Player>
    {
        #region FIELDS

        [Header("Look")]
        [SerializeField] private Transform cameraRoot;
        [SerializeField] private float eyeOffset = 0.15f;
        [SerializeField] private float sensitivity = 0.15f;
        [SerializeField] private float turnRate = 150f;
        [SerializeField, Min(0f)] private float smoothing;

        [Header("Pitch")]
        [SerializeField] private float minPitch = -89f;
        [SerializeField] private float maxPitch = 89f;

        [SerializeField] private Nucleus nucleus = new();

        #endregion

        internal Controls controls;
        private InputAction look;
        private float pitch;
        private Vector3 rest;
        private Vector2 smoothed;

        public Transform Root => cameraRoot;

        public override void Hook(Player owner)
        {
            base.Hook(owner);
            controls = GameManager.Find<Controls>();
            look = controls?.Get(Control.Look);
            if (cameraRoot != null) rest = cameraRoot.localPosition;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            nucleus.Initialize(this);
        }

        public override void Handle()
        {
            var delta = look?.ReadValue<Vector2>() ?? default;
            delta *= look?.activeControl?.device is Gamepad ? turnRate * Time.deltaTime : sensitivity;
            if (smoothing > 0f) delta = smoothed = Vector2.Lerp(smoothed, delta, smoothing * Time.deltaTime);
            Owner.transform.Rotate(0f, delta.x, 0f);
            pitch = Mathf.Clamp(pitch - delta.y, minPitch, maxPitch);
            if (cameraRoot != null)
            {
                var controller = Owner.Controller;
                cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                cameraRoot.localPosition = new Vector3(rest.x, controller.center.y + controller.height * 0.5f - eyeOffset, rest.z);
            }

            nucleus.Handle();
        }

        public override void Unhook()
        {
            nucleus.Teardown();
            look = null;
            controls = null;
            smoothed = default;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            base.Unhook();
        }
    }
}
