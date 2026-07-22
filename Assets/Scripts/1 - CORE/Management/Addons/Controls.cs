using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XInput;

namespace Quark
{
    public enum Control { Move, Look, Jump, Sprint, Crouch, CrouchHold, Interact }

    [Serializable]
    public class Controls : Addon<GameManager>
    {
        #region FIELDS

        [SerializeField] private InputActionAsset actions;
        [SerializeField] private Prompts prompts = new();

        #endregion

        #region LIFETIME

        public override void Hook(GameManager owner)
        {
            base.Hook(owner);
            owner.Values.Set("Controls", this);
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
            if (Owner != null) Owner.Values.Forget("Controls");
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

        public Sprite Icon(Control control) => prompts.Resolve(Get(control), Active);
        public Sprite Icon(Control control, Scheme scheme) => prompts.Resolve(Get(control), scheme);

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
            Keyboard or Mouse => Scheme.KeyboardMouse,
            _ => Scheme.Other,
        };

        #endregion

        #region CLASSES

        public enum Scheme { KeyboardMouse, Gamepad, Xbox, PlayStation, Other }

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

        [Serializable]
        public class Prompts
        {
            [SerializeField] private SpritePalette keyboardMouse;
            [SerializeField] private SpritePalette gamepad;
            [SerializeField] private SpritePalette xbox;
            [SerializeField] private SpritePalette playstation;
            [SerializeField] private Sprite fallback;

            private readonly Dictionary<Scheme, SpritePalette> loaded = new();

            internal Sprite Resolve(InputAction action, Scheme scheme)
            {
                if (action == null) return fallback;
                var palette = For(scheme);
                if (palette == null) return fallback;

                var key = Key(action, scheme);
                return palette.TryGet(Normalize(key), out var sprite) ? sprite : fallback;
            }

            private SpritePalette For(Scheme scheme)
            {
                if (loaded.TryGetValue(scheme, out var palette)) return palette;

                palette = scheme switch
                {
                    Scheme.Xbox => xbox,
                    Scheme.PlayStation => playstation,
                    Scheme.KeyboardMouse => keyboardMouse,
                    _ => gamepad,
                };
                if (palette == null) palette = Resources.Load<SpritePalette>($"Palettes/Input.{scheme}");
                if (palette == null && scheme != Scheme.KeyboardMouse) palette = gamepad != null ? gamepad : Resources.Load<SpritePalette>("Palettes/Input.Gamepad");
                return loaded[scheme] = palette;
            }

            private static string Key(InputAction action, Scheme scheme)
            {
                var group = scheme == Scheme.KeyboardMouse ? "Keyboard" : "Gamepad";
                for (var i = 0; i < action.bindings.Count; i++)
                {
                    var binding = action.bindings[i];
                    if (binding.isComposite || string.IsNullOrEmpty(binding.groups) || !binding.groups.Contains(group, StringComparison.OrdinalIgnoreCase)) continue;
                    if (binding.isPartOfComposite)
                        for (var c = i - 1; c >= 0; c--)
                            if (action.bindings[c].isComposite) return action.bindings[c].name;
                    return Tail(binding.effectivePath);
                }
                return null;
            }

            private static string Tail(string path)
            {
                if (string.IsNullOrEmpty(path)) return null;
                var slash = path.LastIndexOf('/');
                var tail = slash < 0 ? path : path[(slash + 1)..];
                return path.Contains("dpad/") ? "dpad" + char.ToUpperInvariant(tail[0]) + tail[1..] : tail;
            }

            private static string Normalize(string key) => key switch
            {
                "leftShift" or "rightShift" => "shift",
                "leftCtrl" or "rightCtrl" => "ctrl",
                "leftAlt" or "rightAlt" => "alt",
                _ => key,
            };
        }

        #endregion
    }
}
