using System;
using UnityEngine;

namespace Quark
{
    public abstract class Identifiable : MonoBehaviour
    {
        #region FIELDS

        [field: SerializeField] public string Id { get; private set; }

        #endregion

        #region API

        private string uid;
        public string Uid => string.IsNullOrEmpty(uid) ? (uid = Quantum.NewUid()) : uid;

        public event Action<Context> OnFocus;
        public event Action<Context> OnUnfocus;

        public virtual void Focus(Context context) => OnFocus?.Invoke(context);
        public virtual void Unfocus(Context context) => OnUnfocus?.Invoke(context);

        #endregion
    }
}
