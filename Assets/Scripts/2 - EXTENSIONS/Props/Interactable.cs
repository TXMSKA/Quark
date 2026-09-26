using UnityEngine;

namespace Quark
{
    public class Interactable : Prop
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
            if (context != null && context.TryGet(Context.Stage, out var phase) && phase == Context.Phase.Press)
                sfx.Play("item.use");
        }

        public override void Interact(Context context)
        {
            base.Interact(context);
            if (context != null && context.TryGet(Context.Stage, out var phase) && phase == Context.Phase.Press &&
                context.TryGet(Context.Input, out var channel) && channel == Context.Channel.Primary)
                sfx.Play("item.interact");
        }

        #endregion
    }
}
