using UnityEngine;

namespace Quark
{
    public class CrosshairOverride : Mod<Prop>
    {
        #region FIELDS

        [SerializeField] private bool custom;
        [SerializeField, ShowIf(nameof(custom))] private string customId;

        #endregion

        #region LIFETIME

        public override void Hook(Prop owner)
        {
            base.Hook(owner);
            owner.OnFocus += Show;
            owner.OnUnfocus += Hide;
        }

        public override void Unhook()
        {
            if (Owner != null)
            {
                Owner.OnFocus -= Show;
                Owner.OnUnfocus -= Hide;
            }
            crosshair = null;
            base.Unhook();
        }

        #endregion

        #region MISC

        private Crosshair crosshair;

        private Crosshair Target()
        {
            if (crosshair != null) return crosshair;
            var flow = GameManager.Find<Flow>();
            return crosshair = flow != null ? flow.Get<Crosshair>() : null;
        }

        private void Show(Context context) => Target()?.Set(custom ? Crosshair.Id.Custom : Crosshair.Id.Interact, customId);
        private void Hide(Context context) => Target()?.Set(Crosshair.Id.Default);

        #endregion
    }
}
