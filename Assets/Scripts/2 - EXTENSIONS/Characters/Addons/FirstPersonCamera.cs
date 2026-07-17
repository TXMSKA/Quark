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
        [SerializeField] private float sensitivity = 0.15f;
        [SerializeField] private float turnRate = 150f;

        [Header("Pitch")]
        [SerializeField] private float minPitch = -89f;
        [SerializeField] private float maxPitch = 89f;

        #endregion

        private InputAction look;
        private float pitch;

        public override void Hook(Player owner)
        {
            base.Hook(owner);
            if (GameManager.Instance != null && GameManager.Instance.Values.TryGet("Controls", out Controls controls))
                look = controls.Get(Control.Look);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public override void Handle()
        {
            var delta = look?.ReadValue<Vector2>() ?? default;
            delta *= look?.activeControl?.device is Gamepad ? turnRate * Time.deltaTime : sensitivity;
            Owner.transform.Rotate(0f, delta.x, 0f);
            pitch = Mathf.Clamp(pitch - delta.y, minPitch, maxPitch);
            if (cameraRoot != null) cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        public override void Unhook()
        {
            look = null;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            base.Unhook();
        }
    }
}
