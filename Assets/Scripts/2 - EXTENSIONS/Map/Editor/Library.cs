using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Quark
{
    // Folder palettes: a folder is a palette, its subfolders are categories. Drawn over the Scene view.
    [InitializeOnLoad]
    static class Library
    {
        #region FIELDS

        const float Header = 44f, Footer = 28f, Side = 168f, Gap = 8f;

        sealed class Category
        {
            public string name;
            public string[] paths;
        }

        public static bool open;

        static readonly Dictionary<string, Category[]> index = new();
        static readonly Dictionary<string, GameObject> loaded = new();
        static readonly Dictionary<GameObject, Texture2D> previews = new();
        static readonly Dictionary<GameObject, int> wanted = new();
        static readonly List<GameObject> settled = new();
        static readonly Dictionary<string, string> fitted = new();
        static double repaintAt;
        static int palette, category;
        static string search = "";
        static Vector2 scroll, sideScroll, pressedAt;
        static GUIStyle buttonOn, buttonOff, caption, item, itemOn, count;

        static GUIStyle ButtonOn => buttonOn ??= Text(11, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        static GUIStyle ButtonOff => buttonOff ??= Text(11, FontStyle.Bold, Charm.Title, TextAnchor.MiddleCenter);
        static GUIStyle Caption => caption ??= new GUIStyle(Text(10, FontStyle.Normal, Charm.Title, TextAnchor.MiddleCenter)) { clipping = TextClipping.Clip };
        static GUIStyle Item => item ??= Text(11, FontStyle.Normal, Charm.Title, TextAnchor.MiddleLeft);
        static GUIStyle ItemOn => itemOn ??= Text(11, FontStyle.Bold, Charm.Accent, TextAnchor.MiddleLeft);
        static GUIStyle Count => count ??= Text(10, FontStyle.Normal, Charm.Dim, TextAnchor.MiddleRight);
        static Color Edge => new(Charm.Dim.r, Charm.Dim.g, Charm.Dim.b, 0.6f);

        #endregion

        #region LIFETIME

        static Library()
        {
            EditorApplication.projectChanged += () => { index.Clear(); loaded.Clear(); previews.Clear(); Snap.Clear(); };
            EditorApplication.update += Previews;
            AssetPreview.SetPreviewTextureCacheSize(1024);
        }

        #endregion

        #region API

        public static GameObject Load(string path)
        {
            if (!loaded.TryGetValue(path, out var go) || go == null)
                loaded[path] = go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return go;
        }

        // A clearly bordered button; filled with the accent while on.
        public static bool Button(Rect r, string label, bool on = false)
        {
            Charm.Fill(r, on ? Charm.Accent : Charm.Surface2, Charm.Radius);
            Charm.Outline(r, on ? Charm.Accent : Edge, Charm.Radius);
            GUI.Label(r, label, on ? ButtonOn : ButtonOff);
            EditorGUIUtility.AddCursorRect(r, MouseCursor.Link);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        // Draws the library over the scene; returns true while the mouse is over it.
        public static bool Draw(SceneView view)
        {
            if (!open) return false;
            var shelf = Shelf.Instance;
            var size = view.position.size;
            var w = Mathf.Min(size.x - 48f, 860f);
            var h = Mathf.Min(size.y - 64f, 580f);
            var r = new Rect((size.x - w) * 0.5f, Mathf.Max(32f, (size.y - h) * 0.5f), w, h);
            var e = Event.current;

            Handles.BeginGUI();
            Charm.Fill(r, Charm.Surface, 10f);
            Charm.Outline(r, Edge, 10f);
            Top(r, shelf);
            Charm.Rule(r.x, r.y + Header, r.width);

            var body = new Rect(r.x, r.y + Header + 1f, r.width, r.height - Header - Footer - 2f);
            if (shelf.palettes.Count == 0)
                GUI.Label(new Rect(body.x + 16f, body.y + 16f, body.width - 32f, 20f), "Import a folder with + Folder: it becomes a palette and its subfolders become categories.", Charm.Hint);
            else
            {
                var categories = Index(shelf.palettes[palette]);
                category = Mathf.Clamp(category, 0, categories.Length);
                Sidebar(new Rect(body.x, body.y, Side, body.height), categories);
                Charm.Fill(new Rect(body.x + Side, body.y, 1f, body.height), Charm.Line, 0f);
                var items = Items(categories);
                Grid(new Rect(body.x + Side + 12f, body.y + 10f, body.width - Side - 20f, body.height - 14f), items, shelf);
                var bottom = new Rect(r.x + 14f, r.yMax - Footer, r.width - 28f, Footer - 4f);
                Charm.Rule(r.x, r.yMax - Footer - 1f, r.width);
                GUI.Label(bottom, $"{items.Count} pieces  ·  {shelf.Tray.Count} in slot {shelf.slot + 1}", Charm.MiniDim);
                GUI.Label(bottom, "Click adds to the palette  ·  Drag places in the scene", Count);
            }

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) { open = false; e.Use(); }
            var over = r.Contains(e.mousePosition);
            if (over && (e.type == EventType.MouseDown || e.type == EventType.MouseUp || e.type == EventType.MouseDrag || e.type == EventType.ScrollWheel)) e.Use();
            Handles.EndGUI();
            return over;
        }

        // A thumbnail cell with an optional caption. Returns 1 on left click, 2 on right click; dragging it starts a scene drop.
        public static int Cell(Rect r, GameObject go, bool on, string label)
        {
            var e = Event.current;
            var id = GUIUtility.GetControlID(FocusType.Passive, r);
            Charm.Fill(r, on ? Charm.AccentSoft : Charm.Surface2, Charm.Radius);
            Charm.Outline(r, on ? Charm.Accent : Charm.Line, Charm.Radius);

            var thumb = new Rect(r.x + 4f, r.y + 4f, r.width - 8f, r.width - 8f);
            if (e.type == EventType.Repaint)
            {
                Texture preview = Preview(go);
                if (preview != null) GUI.DrawTexture(thumb, preview, ScaleMode.ScaleToFit);
            }
            if (on) Charm.Fill(new Rect(r.xMax - 13f, r.y + 5f, 8f, 8f), Charm.Accent, 4f);
            if (label != null)
            {
                var line = new Rect(r.x + 3f, thumb.yMax, r.width - 6f, r.yMax - thumb.yMax - 2f);
                GUI.Label(line, Fit(label, line.width), Caption);
            }
            GUI.Label(r, new GUIContent("", go.name), GUIStyle.none);
            EditorGUIUtility.AddCursorRect(r, MouseCursor.Link);

            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown when r.Contains(e.mousePosition):
                    if (e.button == 1) { e.Use(); return 2; }
                    if (e.button != 0) break;
                    GUIUtility.hotControl = id;
                    pressedAt = e.mousePosition;
                    e.Use();
                    break;
                case EventType.MouseDrag when GUIUtility.hotControl == id:
                    if ((e.mousePosition - pressedAt).sqrMagnitude < 36f) break;
                    GUIUtility.hotControl = 0;
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.objectReferences = new Object[] { go };
                    DragAndDrop.paths = new[] { AssetDatabase.GetAssetPath(go) };
                    DragAndDrop.StartDrag(go.name);
                    e.Use();
                    break;
                case EventType.MouseUp when GUIUtility.hotControl == id:
                    GUIUtility.hotControl = 0;
                    e.Use();
                    if (r.Contains(e.mousePosition)) return 1;
                    break;
            }
            return 0;
        }

        #endregion

        #region MISC

        // Previews are rendered by Unity; asking for them inside OnGUI breaks the frame, so they are requested from update.
        static Texture Preview(GameObject go)
        {
            if (previews.TryGetValue(go, out var texture) && texture != null) return texture;
            if (!wanted.ContainsKey(go)) wanted[go] = 0;
            return AssetPreview.GetMiniThumbnail(go);
        }

        static void Previews()
        {
            if (wanted.Count == 0) return;
            settled.Clear();
            var keys = new List<GameObject>(wanted.Keys);
            foreach (var go in keys)
            {
                var texture = go != null ? AssetPreview.GetAssetPreview(go) : null;
                if (texture != null) previews[go] = texture;
                if (texture != null || go == null || ++wanted[go] > 600) settled.Add(go);
            }
            foreach (var go in settled) wanted.Remove(go);
            if (settled.Count == 0 || EditorApplication.timeSinceStartup < repaintAt) return;
            repaintAt = EditorApplication.timeSinceStartup + 0.25;
            SceneView.RepaintAll();
        }

        static GUIStyle Text(int size, FontStyle style, Color color, TextAnchor anchor) =>
            new(EditorStyles.label) { fontSize = size, fontStyle = style, alignment = anchor, normal = { textColor = color } };

        static void Top(Rect r, Shelf shelf)
        {
            var y = r.y + 10f;
            var x = r.x + 14f;
            var names = new string[shelf.palettes.Count];
            for (var i = 0; i < names.Length; i++) names[i] = Path.GetFileName(shelf.palettes[i]);
            palette = Mathf.Clamp(palette, 0, Mathf.Max(0, names.Length - 1));

            if (names.Length > 0)
            {
                var next = EditorGUI.Popup(new Rect(x, y + 3f, 150f, 18f), palette, names);
                if (next != palette) { palette = next; category = 0; scroll = Vector2.zero; }
                x += 158f;
            }
            if (Button(new Rect(x, y, 78f, 24f), "+ Folder")) Import(shelf);
            x += 84f;
            if (names.Length > 0 && Button(new Rect(x, y, 70f, 24f), "Remove"))
            {
                shelf.Edit("Remove Palette");
                shelf.palettes.RemoveAt(palette);
                palette = 0;
            }

            var right = r.xMax - 14f;
            if (Button(new Rect(right - 24f, y, 24f, 24f), "X")) open = false;
            right -= 34f;
            var icon = GUI.HorizontalSlider(new Rect(right - 80f, y + 5f, 80f, 14f), shelf.libraryIcon, 48f, 144f);
            if (!Mathf.Approximately(icon, shelf.libraryIcon)) { shelf.Edit("Library Size"); shelf.libraryIcon = icon; }
            GUI.Label(new Rect(right - 112f, y, 30f, 24f), "Size", Charm.MiniDim);
            right -= 122f;
            search = EditorGUI.TextField(new Rect(right - 180f, y + 3f, 180f, 18f), search, EditorStyles.toolbarSearchField);
        }

        static void Sidebar(Rect r, Category[] categories)
        {
            const float row = 26f;
            var content = new Rect(0f, 0f, r.width - 14f, (categories.Length + 1) * row + 12f);
            var scrolls = content.height > r.height;
            if (!scrolls) content.width = r.width;
            sideScroll = GUI.BeginScrollView(r, sideScroll, content);
            var total = 0;
            foreach (var c in categories) total += c.paths.Length;
            for (var i = 0; i <= categories.Length; i++)
            {
                var line = new Rect(8f, 6f + i * row, content.width - 16f, row - 2f);
                var on = i == category && string.IsNullOrEmpty(search);
                if (on) Charm.Fill(line, Charm.AccentSoft, Charm.Radius);
                var label = i == 0 ? "All" : categories[i - 1].name;
                var n = i == 0 ? total : categories[i - 1].paths.Length;
                GUI.Label(new Rect(line.x + 8f, line.y, line.width - 44f, line.height), label, on ? ItemOn : Item);
                GUI.Label(new Rect(line.x, line.y, line.width - 8f, line.height), n.ToString(), Count);
                EditorGUIUtility.AddCursorRect(line, MouseCursor.Link);
                if (GUI.Button(line, GUIContent.none, GUIStyle.none)) { category = i; search = ""; scroll = Vector2.zero; GUI.FocusControl(null); }
            }
            GUI.EndScrollView();
        }

        static List<string> Items(Category[] categories)
        {
            var items = new List<string>();
            var filtered = !string.IsNullOrEmpty(search);
            for (var i = 0; i < categories.Length; i++)
            {
                if (!filtered && category != 0 && category != i + 1) continue;
                foreach (var path in categories[i].paths)
                    if (!filtered || Path.GetFileNameWithoutExtension(path).IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0) items.Add(path);
            }
            return items;
        }

        static void Grid(Rect r, List<string> items, Shelf shelf)
        {
            var columns = Mathf.Max(1, Mathf.FloorToInt((r.width - 14f + Gap) / (shelf.libraryIcon + Gap)));
            var cellW = (r.width - 14f - Gap * (columns - 1)) / columns;
            var cellH = cellW + 16f;
            var rows = Mathf.CeilToInt(items.Count / (float)columns);
            var content = new Rect(0f, 0f, r.width - 14f, rows * (cellH + Gap));
            var prefix = Prefix(items);

            scroll = GUI.BeginScrollView(r, scroll, content);
            var first = Mathf.Max(0, Mathf.FloorToInt(scroll.y / (cellH + Gap)) * columns);
            var last = Mathf.Min(items.Count, Mathf.CeilToInt((scroll.y + r.height) / (cellH + Gap)) * columns);
            for (var i = first; i < last; i++)
            {
                var go = Load(items[i]);
                if (go == null) continue;
                var cell = new Rect(i % columns * (cellW + Gap), i / columns * (cellH + Gap), cellW, cellH);
                if (Cell(cell, go, shelf.Tray.Contains(go), Pretty(go.name, prefix)) != 1) continue;
                shelf.Edit("Edit Palette");
                if (!shelf.Tray.Remove(go)) shelf.Tray.Add(go);
            }
            GUI.EndScrollView();
        }

        // Shared leading token (like a kit name) that every item repeats; hidden from captions.
        static string Prefix(List<string> items)
        {
            if (items.Count == 0) return "";
            var first = Path.GetFileNameWithoutExtension(items[0]);
            var cut = first.IndexOf('_');
            if (cut <= 0) return "";
            var prefix = first.Substring(0, cut + 1);
            foreach (var path in items)
                if (!Path.GetFileNameWithoutExtension(path).StartsWith(prefix)) return "";
            return prefix;
        }

        // One line: when the caption is too wide, the first words go, since the end tells pieces apart.
        static string Fit(string label, float width)
        {
            var key = label + "|" + Mathf.RoundToInt(width);
            if (fitted.TryGetValue(key, out var text)) return text;
            text = label;
            while (Caption.CalcSize(new GUIContent(text)).x > width)
            {
                var space = text.IndexOf(' ', text.StartsWith("…") ? 1 : 0);
                if (space < 0) break;
                text = "…" + text.Substring(space + 1);
            }
            return fitted[key] = text;
        }

        static string Pretty(string name, string prefix)
        {
            if (prefix.Length > 0 && name.StartsWith(prefix)) name = name.Substring(prefix.Length);
            var text = new StringBuilder(name.Length + 8);
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (c == '_' || c == '-') { text.Append(' '); continue; }
                if (char.IsUpper(c) && i > 0 && char.IsLower(name[i - 1])) text.Append(' ');
                text.Append(char.ToLowerInvariant(c));
            }
            return text.ToString();
        }

        static void Import(Shelf shelf)
        {
            var chosen = EditorUtility.OpenFolderPanel("Import palette folder", "Assets", "");
            if (string.IsNullOrEmpty(chosen)) return;
            chosen = chosen.Replace('\\', '/');
            var data = Application.dataPath.Replace('\\', '/');
            if (!chosen.StartsWith(data)) { EditorUtility.DisplayDialog("Import palette", "Pick a folder inside this project's Assets.", "OK"); return; }
            var path = "Assets" + chosen.Substring(data.Length);
            if (shelf.palettes.Contains(path)) { palette = shelf.palettes.IndexOf(path); return; }
            shelf.Edit("Import Palette");
            shelf.palettes.Add(path);
            palette = shelf.palettes.Count - 1;
            category = 0;
        }

        static Category[] Index(string folder)
        {
            if (index.TryGetValue(folder, out var categories)) return categories;
            var list = new List<Category>();
            var own = Find(folder, false);
            if (own.Length > 0) list.Add(new Category { name = Path.GetFileName(folder), paths = own });
            if (AssetDatabase.IsValidFolder(folder))
                foreach (var sub in AssetDatabase.GetSubFolders(folder))
                {
                    var paths = Find(sub, true);
                    if (paths.Length > 0) list.Add(new Category { name = Path.GetFileName(sub), paths = paths });
                }
            return index[folder] = list.ToArray();
        }

        static string[] Find(string folder, bool deep)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return new string[0];
            var paths = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:GameObject", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (deep || Path.GetDirectoryName(path).Replace('\\', '/') == folder) paths.Add(path);
            }
            paths.Sort();
            return paths.ToArray();
        }

        #endregion
    }
}
