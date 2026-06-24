using UnityEngine;

namespace Quark
{
    public class Trackable : MonoBehaviour
    {
        #region FIELDS
        [field: SerializeField] public string Id { get; private set; }
        #endregion

        #region API
        string uid;
        public string Uid => string.IsNullOrEmpty(uid) ? (uid = Quantum.NewUid()) : uid;
        #endregion
    }
}
