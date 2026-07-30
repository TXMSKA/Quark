using UnityEngine;

namespace Quark
{
    public class Item : Prop
    {
        #region FIELDS

        [SerializeField] private SFXEmitter sfx = new();

        #endregion

        #region LIFETIME

        protected override void Awake()
        {
            base.Awake();
            sfx.Bind(transform);
        }

        #endregion

        #region API

        public override void Use(Context context)
        {
            base.Use(context);
            sfx.Play("item.use");
        }

        public override void Interact(Context context)
        {
            base.Interact(context);
            sfx.Play("item.interact");
        }

        #endregion
    }
}
