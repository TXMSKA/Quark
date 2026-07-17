using System;
using UnityEngine;

namespace Quark
{
    [Serializable]
    public class Tick : Addon<GameManager>
    {
        #region FIELDS

        [Header("Tick")]
        [field: SerializeField] public bool StartsTicking { get; private set; } = true;
        [field: SerializeField, Min(0.01f)] public float Rate { get; private set; } = 1f;

        #endregion

        #region LIFETIME

        public override void Hook(GameManager owner)
        {
            base.Hook(owner);
            owner.Values.Set("Tick", this);
            Set(StartsTicking);
        }

        public override void Handle()
        {
            if (!Ticking) return;
            accumulator += Time.deltaTime * Rate;
            while (accumulator >= 1f)
            {
                accumulator -= 1f;
                OnTick?.Invoke();
            }
        }
        private float accumulator;

        public override void Unhook()
        {
            Set(false);
            if (Owner != null) Owner.Values.Forget("Tick");
            base.Unhook();
        }

        #endregion

        #region API

        public bool Ticking { get; private set; }
        public float RealDelta => 1f / Rate;
        public event Action OnTick;

        public void Set(bool ticking) => Ticking = ticking;

        #endregion
    }
}
