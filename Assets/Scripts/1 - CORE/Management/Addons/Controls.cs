using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XInput;

namespace Quark
{
    public enum Control { Move, Look, Jump, Walk, Sprint, SprintToggle, Crouch, CrouchHold, Use, Interact }

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
            owner.Values.Set(nameof(Controls), this);
            InputSystem.onEvent += Detect;
            if (actions == null) { Debug.LogWarning("Controls: no InputActionAsset assigned."); return; }
            foreach (var action in actions)
            {
                action.performed += events.Performed;
                action.canceled += events.Released;
            }
            actions.Enable();
        }

        public override void Unhook()
        {
            InputSystem.onEvent -= Detect;
            if (Owner != null) Owner.Values.Forget(nameof(Controls));
            if (actions != null)
            {
                actions.Disable();
                foreach (var action in actions)
                {
                    action.performed -= events.Performed;
                    action.canceled -= events.Released;
                }
            }
            events.Clear();
            cache.Clear();
            base.Unhook();
        }

        #endregion

        #region API

        public Scheme Active { get; private set; } = Scheme.KeyboardMouse;
        public event Action<Scheme> OnSchemeChanged;

        public InputAction Get(Control control)
        {
            if (!cache.TryGetValue(control, out var action) && actions != null)
                cache[control] = action = actions.FindAction(control.ToString(), true);
            return action;
        }

        public Vector2 Axis(Control control) => Get(control)?.ReadValue<Vector2>() ?? default;

        public void Subscribe(Control control, Action onPerformed, Action onReleased = null) => events.Subscribe(control, onPerformed, onReleased);
        public void Unsubscribe(Control control, Action onPerformed, Action onReleased = null) => events.Unsubscribe(control, onPerformed, onReleased);

        #endregion

        #region MISC

        private readonly Events events = new();
        private readonly Dictionary<Control, InputAction> cache = new();

        private void Detect(InputEventPtr eventPtr, InputDevice device)
        {
            if (device == null || !eventPtr.valid) return;
            var next = Classify(device);
            if (next == Scheme.Other || next == Active) return;
            Active = next;
            OnSchemeChanged?.Invoke(next);
        }

        private static Scheme Classify(InputDevice device) => device switch
        {
            XInputController => Scheme.Xbox,
            DualShockGamepad => Scheme.PlayStation,
            Gamepad => Scheme.Gamepad,
            TrackedDevice => Scheme.XR,
            Keyboard or Mouse => Scheme.KeyboardMouse,
            _ => Scheme.Other,
        };

        #endregion

        #region CLASSES

        public enum Scheme { KeyboardMouse, Gamepad, Xbox, PlayStation, XR, Other }

        private class Events
        {
            private readonly Dictionary<string, Action> performed = new(), released = new();

            public void Subscribe(Control control, Action onPerformed, Action onReleased)
            {
                Add(performed, control, onPerformed);
                Add(released, control, onReleased);
            }

            public void Unsubscribe(Control control, Action onPerformed, Action onReleased)
            {
                Remove(performed, control, onPerformed);
                Remove(released, control, onReleased);
            }

            public void Performed(InputAction.CallbackContext context) => Dispatch(performed, context);
            public void Released(InputAction.CallbackContext context) => Dispatch(released, context);

            public void Clear()
            {
                performed.Clear();
                released.Clear();
            }

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
        }

        #endregion
    }
}
