using System;
using UnityEngine;

namespace Quark
{
    public class Hub : Service
    {
        #region FIELDS

        [SerializeField] private Zone[] zones = Array.Empty<Zone>();

        #endregion

        #region LIFETIME

        protected override void Awake()
        {
            base.Awake();
            foreach (var zone in zones)
                if (zone != null && zone.root != null && zone.requirement == null)
                    zone.root.SetActive(false);
        }

        #endregion

        [Serializable]
        private sealed class Zone
        {
            public GameObject root;
            [Tooltip("A missing requirement hides this zone. A disabled component still counts as present.")]
            public MonoBehaviour requirement;
        }
    }
}
