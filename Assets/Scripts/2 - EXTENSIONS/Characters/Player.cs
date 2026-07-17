using UnityEngine;

namespace Quark
{
    [RequireComponent(typeof(CharacterController))]
    public class Player : Entity
    {
        #region FIELDS

        public CharacterController Controller { get; private set; }

        #endregion

        #region LIFETIME

        private void Reset() => Controller = GetComponent<CharacterController>();

        protected override void Awake()
        {
            base.Awake();
            Controller = GetComponent<CharacterController>();
        }

        #endregion
    }
}
