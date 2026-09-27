using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Quark
{
    public enum Control { Move, Look, Jump, Walk, Sprint, SprintToggle, Crouch, CrouchHold, Use, Interact, Secondary }

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
            owner.Values.Set(Key<Controls>.Default, this);
            if (actions == null) { Debug.LogWarning("Controls: no InputActionAsset assigned."); return; }
            foreach (var action in actions)
            {
                action.performed += Performed;
                action.canceled += Released;
            }
            actions.Enable();
        }

        public override void Unhook()
        {
            if (Owner != null) Owner.Values.Forget(Key<Controls>.Default);
            if (actions != null)
            {
                actions.Disable();
                foreach (var action in actions)
                {
                    action.performed -= Performed;
                    action.canceled -= Released;
                }
            }
            performed.Clear();
            released.Clear();
            cache.Clear();
            base.Unhook();
        }

        #endregion

        #region API

        public InputAction Get(Control control)
        {
            if (!cache.TryGetValue(control, out var action) && actions != null)
                cache[control] = action = actions.FindAction(control.ToString(), true);
            return action;
        }

        public Vector2 Axis(Control control) => Get(control)?.ReadValue<Vector2>() ?? default;

        public void Subscribe(Control control, Action onPerformed, Action onReleased = null)
        {
            var name = control.ToString();
            performed[name] = performed.GetValueOrDefault(name) + onPerformed;
            released[name] = released.GetValueOrDefault(name) + onReleased;
        }

        public void Unsubscribe(Control control, Action onPerformed, Action onReleased = null)
        {
            var name = control.ToString();
            performed[name] = performed.GetValueOrDefault(name) - onPerformed;
            released[name] = released.GetValueOrDefault(name) - onReleased;
        }

        #endregion

        #region MISC

        private readonly Dictionary<string, Action> performed = new();
        private readonly Dictionary<string, Action> released = new();
        private readonly Dictionary<Control, InputAction> cache = new();

        private void Performed(InputAction.CallbackContext context) =>
            performed.GetValueOrDefault(context.action.name)?.Invoke();

        private void Released(InputAction.CallbackContext context) =>
            released.GetValueOrDefault(context.action.name)?.Invoke();

        #endregion
    }
}
