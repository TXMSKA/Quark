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

        protected override void Awake()
        {
            base.Awake();
            Controller = GetComponent<CharacterController>();
        }

        #endregion
    }
}
