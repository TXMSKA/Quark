using System;
using UnityEngine;
using UnityEngine.UI;

namespace Quark
{
    [Serializable]
    public class Crosshair : Addon<Flow>
    {
        #region FIELDS

        [SerializeField] private Image image;
        [SerializeField] private SpritePalette icons;

        #endregion

        #region LIFETIME

        public override void Hook(Flow owner)
        {
            base.Hook(owner);
            Set(Id.Default);
        }

        #endregion

        #region API

        public enum Id { Default, Interact, Custom }

        public void Set(Id id, string custom = null)
        {
            if (image == null || icons == null) return;
            var found = icons.TryGet(id == Id.Custom ? custom : id.ToString(), out var sprite);
            image.enabled = found;
            if (found) image.sprite = sprite;
        }

        #endregion
    }
}
