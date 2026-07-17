using UnityEngine;

namespace Quark
{
    public class TestProp : Prop
    {
        [ContextMenu("Focus")] void TestFocus() => Focus(new Context());
        [ContextMenu("Unfocus")] void TestUnfocus() => Unfocus(new Context());
        [ContextMenu("Use")] void TestUse() => Use(new Context());
        [ContextMenu("Interact")] void TestInteract() => Interact(new Context());
    }
}
