using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace Quark
{
    // Scene view panel of the map builder: mode, grid, placement or brush, and the working palette.
    // It resizes freely; the content follows its width and the palette fills the remaining height.
    [Overlay(typeof(SceneView), "quark-map-panel", "Map", true, defaultDockZone = DockZone.RightColumn)]
    sealed class Panel : Overlay, ITransientOverlay
    {
        #region FIELDS

        const float Width = 140f, Gap = 4f, MinTray = 80f;
        static readonly string[] Modes = { "Place", "Paint", "Move" };
        static GUIStyle hint, value, empty;
        static GUIStyle Value => value ??= new GUIStyle(Charm.MiniAccent) { alignment = TextAnchor.MiddleRight };
        static GUIStyle Hint => hint ??= new GUIStyle(Charm.MiniDim) { wordWrap = true, fontSize = 9 };
        static GUIStyle Empty => empty ??= new GUIStyle(Charm.Hint) { wordWrap = true, alignment = TextAnchor.MiddleCenter };
        static float wide = Width;

        IMGUIContainer gui;
        Vector2 scroll;
        float trayLimit = 300f, trayUsed, rest = 330f;

        public bool visible => ToolManager.activeToolType == typeof(Builder);

        #endregion

        #region LIFETIME

        public override void OnCreated()
        {
            minSize = new Vector2(Width + 24f, 260f);
            maxSize = new Vector2(640f, 4000f);
        }

        public override VisualElement CreatePanelContent()
        {
            gui = new IMGUIContainer(Draw);
            gui.style.flexGrow = 1f;
            return gui;
        }

        #endregion

        #region MISC

        void Draw()
        {
            var shelf = Shelf.Instance;
            var box = gui.contentRect;
            if (box.width > 1f) wide = Mathf.Max(100f, box.width);
            var parent = gui.parent;
            var available = floating
                ? parent != null && parent.contentRect.height > 1f ? parent.contentRect.height : size.y - 34f
                : containerWindow != null ? containerWindow.position.height - gui.worldBound.y - 8f : rest + trayLimit;
            trayLimit = Mathf.Max(MinTray, available - rest);

            GUILayout.BeginVertical(GUILayout.Width(wide));
            ModeRow();
            Divider();
            GridRows(shelf);
            if (Builder.Mode == Builder.Tool.Place) PlaceRows();
            if (Builder.Mode == Builder.Tool.Paint) Brush(shelf);
            Divider();
            Tray(shelf);
            GUILayout.Space(6f);
            if (Library.Button(Row(24f), Library.open ? "Close library" : "Library", Library.open)) Library.open = !Library.open;
            GUILayout.Space(6f);
            GUILayout.Label("B tool  ·  Shift free\nU I O turn  ·  + - scale\nMiddle tap: menu", Hint);
            GUILayout.EndVertical();

            if (Event.current.type != EventType.Repaint) return;
            var measured = GUILayoutUtility.GetLastRect().height - trayUsed;
            if (Mathf.Abs(measured - rest) < 1f) return;
            rest = measured;
            gui.MarkDirtyRepaint();
        }

        static Rect Row(float height) => GUILayoutUtility.GetRect(wide, height);

        static void Divider()
        {
            GUILayout.Space(6f);
            var r = Row(1f);
            Charm.Rule(r.x, r.y, r.width);
            GUILayout.Space(6f);
        }

        static void ModeRow()
        {
            var r = Row(24f);
            var w = (r.width - Gap * 2f) / Modes.Length;
            for (var i = 0; i < Modes.Length; i++)
                if (Library.Button(new Rect(r.x + i * (w + Gap), r.y, w, r.height), Modes[i], (int)Builder.Mode == i))
                    Builder.Mode = (Builder.Tool)i;
        }

        static void GridRows(Shelf shelf)
        {
            var r = Row(20f);
            GUI.Label(new Rect(r.x, r.y, 60f, r.height), "Snap", Charm.MiniDim);
            var snap = Charm.Switch(new Rect(r.xMax - 30f, r.y, 30f, r.height), shelf.snap);
            if (snap != shelf.snap) { shelf.Edit("Toggle Snap"); shelf.snap = snap; }

            GUILayout.Space(2f);
            r = Row(20f);
            GUI.Label(new Rect(r.x, r.y, 36f, r.height), "Grid", Charm.MiniDim);
            var size = EditorGUI.DelayedFloatField(new Rect(r.x + 36f, r.y + 1f, 52f, 18f), Snap.Grid.x);
            if (!Mathf.Approximately(size, Snap.Grid.x) && size > 0.01f) Builder.SetGrid(size);
            if (Library.Button(new Rect(r.xMax - 46f, r.y, 46f, r.height), "Fit")) Builder.MatchGrid();
        }

        static void PlaceRows()
        {
            GUILayout.Space(2f);
            var r = Row(20f);
            GUI.Label(new Rect(r.x, r.y, 36f, r.height), "Turn", Charm.MiniDim);
            GUI.Label(new Rect(r.x + 36f, r.y, r.width - 84f, r.height), Builder.Turned, Charm.MiniAccent);
            if (Library.Button(new Rect(r.xMax - 46f, r.y, 46f, r.height), "Reset")) Builder.ResetTurn();

            GUILayout.Space(2f);
            r = Row(20f);
            GUI.Label(new Rect(r.x, r.y, 36f, r.height), "Scale", Charm.MiniDim);
            GUI.Label(new Rect(r.x + 36f, r.y, r.width - 84f, r.height), $"x {Builder.Scale:0.00}", Charm.MiniAccent);
            if (Library.Button(new Rect(r.xMax - 46f, r.y, 46f, r.height), "Reset")) Builder.Scale = 1f;
        }

        static void Brush(Shelf shelf)
        {
            EditorGUI.BeginChangeCheck();
            var radius = Slider("Radius", shelf.radius, 0.5f, 30f, "0.0");
            var strength = Slider("Strength", shelf.strength, 0f, 1f, "0.00");
            var slope = Slider("Slope", shelf.slope, 0f, 90f, "0");
            float min = shelf.scale.x, max = shelf.scale.y;
            Caption("Scale", $"{min:0.00} - {max:0.00}");
            EditorGUI.MinMaxSlider(Row(16f), ref min, ref max, 0.1f, 3f);
            if (!EditorGUI.EndChangeCheck()) return;
            shelf.Edit("Edit Brush");
            shelf.radius = radius;
            shelf.strength = strength;
            shelf.slope = slope;
            shelf.scale = new Vector2(min, max);
        }

        static float Slider(string label, float value, float min, float max, string format)
        {
            Caption(label, value.ToString(format));
            return GUI.HorizontalSlider(Row(16f), value, min, max);
        }

        static void Caption(string label, string text)
        {
            GUILayout.Space(4f);
            var r = Row(14f);
            GUI.Label(r, label, Charm.MiniDim);
            GUI.Label(r, text, Value);
        }

        void Tray(Shelf shelf)
        {
            Slots(shelf);
            GUILayout.Space(6f);
            shelf.Tray.RemoveAll(go => go == null);
            var r = Row(18f);
            GUI.Label(r, $"Palette  {shelf.Tray.Count}", Charm.MiniDim);
            for (var i = 1; i <= 3; i++)
            {
                var c = new Rect(r.xMax - (4 - i) * 24f + 2f, r.y, 22f, r.height);
                if (!Library.Button(c, new string('I', i), shelf.paletteColumns == i) || shelf.paletteColumns == i) continue;
                shelf.Edit("Palette Columns");
                shelf.paletteColumns = i;
            }
            GUILayout.Space(4f);
            if (Builder.Picks.Count > 0)
            {
                if (Library.Button(Row(20f), $"Clear selection  {Builder.Picks.Count}")) Builder.Release();
                GUILayout.Space(4f);
            }

            // The palette always takes the remaining height, on an inset well, so an empty or short palette still looks placed.
            trayUsed = trayLimit;
            var view = Row(trayUsed);
            Charm.Fill(view, Charm.Inset, Charm.Radius);
            if (shelf.Tray.Count == 0)
            {
                GUI.Label(new Rect(view.x + 10f, view.y, view.width - 20f, Mathf.Min(view.height, 80f)), "Empty palette.\nOpen the library to pick pieces.", Empty);
                return;
            }

            var inner = new Rect(view.x + Gap, view.y + Gap, view.width - Gap * 2f, view.height - Gap * 2f);
            var columns = Mathf.Clamp(shelf.paletteColumns, 1, 3);
            var cell = (inner.width - Gap * (columns - 1)) / columns;
            var rows = Mathf.CeilToInt(shelf.Tray.Count / (float)columns);
            var content = rows * (cell + Gap) - Gap;
            var scrolls = content > inner.height;
            if (scrolls)
            {
                cell = (inner.width - 14f - Gap * (columns - 1)) / columns;
                content = rows * (cell + Gap) - Gap;
                scroll = GUI.BeginScrollView(inner, scroll, new Rect(0f, 0f, inner.width - 14f, content));
            }

            var origin = scrolls ? Vector2.zero : inner.position;
            for (var i = 0; i < shelf.Tray.Count; i++)
            {
                var go = shelf.Tray[i];
                var c = new Rect(origin.x + i % columns * (cell + Gap), origin.y + i / columns * (cell + Gap), cell, cell);
                var click = Library.Cell(c, go, Builder.Picks.Contains(go), null);
                if (click == 1) Builder.Pick(go);
                if (click != 2) continue;
                shelf.Edit("Edit Palette");
                shelf.Tray.Remove(go);
                Builder.Picks.Remove(go);
                break;
            }
            if (scrolls) GUI.EndScrollView();
        }

        static void Slots(Shelf shelf)
        {
            var r = Row(22f);
            var n = shelf.slots.Length;
            var w = (r.width - Gap * (n - 1)) / n;
            for (var i = 0; i < n; i++)
            {
                var cell = new Rect(r.x + i * (w + Gap), r.y, w, r.height);
                if (!Library.Button(cell, (i + 1).ToString(), i == shelf.slot) || i == shelf.slot) continue;
                shelf.Edit("Switch Slot");
                shelf.slot = i;
                Builder.Release();
            }
        }

        #endregion
    }
}
