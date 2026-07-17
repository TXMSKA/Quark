using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Quark
{
    public enum Control { Move, Look, Jump, Sprint, Crouch, CrouchHold }

    [Serializable]
    public class Controls : Addon<GameManager>
    {
        #region FIELDS

        [SerializeField] private InputActionAsset actions;

        #endregion

        #region LIFETIME

        public override void Hook(GameManager owner)
        {
            base.Hook(owner);
            owner.Values.Set("Controls", this);
            if (actions == null) { Debug.LogWarning("Controls: no InputActionAsset assigned."); return; }
            foreach (var action in actions)
            {
                action.performed += DispatchPerformed;
                action.canceled += DispatchReleased;
            }
            actions.Enable();
        }

        public override void Unhook()
        {
            if (Owner != null) Owner.Values.Forget("Controls");
            if (actions != null)
            {
                actions.Disable();
                foreach (var action in actions)
                {
                    action.performed -= DispatchPerformed;
                    action.canceled -= DispatchReleased;
                }
            }
            performed.Clear();
            released.Clear();
            base.Unhook();
        }

        #endregion

        #region API

        public InputAction Get(Control control) => actions != null ? actions.FindAction(control.ToString(), true) : null;

        public void Subscribe(Control control, Action onPerformed, Action onReleased = null)
        {
            Add(performed, control, onPerformed);
            Add(released, control, onReleased);
        }

        public void Unsubscribe(Control control, Action onPerformed, Action onReleased = null)
        {
            Remove(performed, control, onPerformed);
            Remove(released, control, onReleased);
        }

        #endregion

        #region MISC

        private readonly Dictionary<string, Action> performed = new(), released = new();

        private void DispatchPerformed(InputAction.CallbackContext context) => Dispatch(performed, context);
        private void DispatchReleased(InputAction.CallbackContext context) => Dispatch(released, context);

        private static void Dispatch(Dictionary<string, Action> bus, InputAction.CallbackContext context)
        {
            if (bus.TryGetValue(context.action.name, out var callback)) callback?.Invoke();
        }

        private static void Add(Dictionary<string, Action> bus, Control control, Action callback)
        {
            if (callback == null) return;
            bus.TryGetValue(control.ToString(), out var current);
            bus[control.ToString()] = current + callback;
        }

        private static void Remove(Dictionary<string, Action> bus, Control control, Action callback)
        {
            if (bus.TryGetValue(control.ToString(), out var current)) bus[control.ToString()] = current - callback;
        }

        #endregion
    }
}
