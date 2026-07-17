using UnityEngine;

namespace Quark
{
    public class TestEntity : Entity
    {
        [ContextMenu("Focus")] void TestFocus() => Focus(new Context());
        [ContextMenu("Unfocus")] void TestUnfocus() => Unfocus(new Context());
    }
}
