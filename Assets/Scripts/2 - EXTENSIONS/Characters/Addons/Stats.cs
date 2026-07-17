using System;
using System.Collections.Generic;
using UnityEngine;

namespace Quark
{
    [Serializable]
    public class Stats : Addon<Entity>
    {
        #region FIELDS

        [SerializeField] private List<Stat> stats = new();

        #endregion

        #region LIFETIME

        public override void Hook(Entity owner)
        {
            base.Hook(owner);
            lookup.Clear();
            foreach (var stat in stats)
            {
                if (!lookup.TryAdd(stat.Key, stat))
                    Debug.LogWarning($"Stats: duplicate key '{stat.Key}' on {owner.name}.", owner);
                stat.Apply(stat.StartingValue);
            }
            if (GameManager.Instance != null && GameManager.Instance.Values.TryGet("Tick", out tick))
                tick.OnTick += OnTick;
        }

        public override void Handle()
        {
            if (tick != null) return;
            Step(Time.deltaTime);
        }

        public override void Unhook()
        {
            if (tick != null) { tick.OnTick -= OnTick; tick = null; }
            lookup.Clear();
            base.Unhook();
        }

        #endregion

        #region API

        public bool TryGet(string key, out Stat stat) => lookup.TryGetValue(key, out stat);
        public bool TryGet(Stat.Kind kind, out Stat stat) => TryGet(kind.ToString(), out stat);
        public Stat Get(string key) => TryGet(key, out var stat) ? stat : throw new KeyNotFoundException(key);
        public Stat Get(Stat.Kind kind) => Get(kind.ToString());

        #endregion

        #region MISC

        private readonly Dictionary<string, Stat> lookup = new();
        private Tick tick;

        private void OnTick() => Step(tick.RealDelta);
        private void Step(float delta) { foreach (var stat in stats) stat.Step(delta); }

        #endregion

        [Serializable]
        public class Stat
        {
            public enum Kind { Health, Stamina, Hunger, Thirst, Energy, Custom }

            #region FIELDS

            [Header("Identity")]
            [SerializeField] private Kind kind;
            [SerializeField] private string customId;

            [Header("Tuning")]
            [field: SerializeField] public float StartingValue { get; private set; } = 100f;
            [field: SerializeField, Min(0f)] public float MaxValue { get; set; } = 100f;
            [field: SerializeField] public float Rate { get; set; }
            [field: SerializeField, Min(0f)] public float Cooldown { get; set; }

            #endregion

            #region API

            public Action<float> OnChanged;
            public Action OnDeplete;

            public string Key => kind == Kind.Custom ? customId : kind.ToString();

            public float Value
            {
                get => current;
                set { cooldownLeft = Cooldown; Apply(value); }
            }

            #endregion

            #region MISC

            private float current;
            private float cooldownLeft;

            internal void Step(float delta)
            {
                cooldownLeft = Mathf.Max(0f, cooldownLeft - delta);
                if (cooldownLeft > 0f || Rate == 0f) return;
                Apply(current + Rate * delta);
            }

            internal void Apply(float next)
            {
                next = Mathf.Clamp(next, 0f, MaxValue);
                if (Mathf.Approximately(next, current)) return;
                current = next;
                OnChanged?.Invoke(next);
                if (next == 0f) OnDeplete?.Invoke();
            }

            #endregion
        }
    }
}
