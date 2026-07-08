using UnityEngine;

namespace Quark
{
    public abstract class Service : MonoBehaviour
    {
        #region FIELDS

        [field: SerializeField] public int Priority { get; private set; }
        [SerializeField] Nucleus<Service> nucleus = new();

        #endregion

        #region LIFETIME

        protected virtual void Awake() => nucleus.Initialize(this);
        protected virtual void OnDestroy() => nucleus.Teardown();

        #endregion
    }
}
