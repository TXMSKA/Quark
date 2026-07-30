using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Quark
{
    // Hierarchy skin: per-object row tint + icon, vHierarchy-style. Children inherit the tint, softer.
    [InitializeOnLoad]
    static class CharmHierarchy
    {
        static readonly Dictionary<int, string> ids = new();
        static Texture2D gradient;
        static GUIStyle label;

        static CharmHierarchy()
        {
            EditorApplication.hierarchyWindowItemOnGUI += Draw;
            EditorApplication.hierarchyChanged += ids.Clear;
        }

        internal static string Id(GameObject go)
        {
            var key = go.GetInstanceID();
            if (!ids.TryGetValue(key, out var id))
                ids[key] = id = GlobalObjectId.GetGlobalObjectIdSlow(go).ToString();
            return id;
        }

        static Texture2D Gradient
        {
            get
            {
                if (gradient != null) return gradient;
                gradient = new Texture2D(64, 1) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
                for (var x = 0; x < 64; x++)
                    gradient.SetPixel(x, 0, new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0.25f, x / 63f)));
                gradient.Apply();
                return gradient;
            }
        }

        static void Draw(int instanceID, Rect rect)
        {
            if (EditorUtility.InstanceIDToObject(instanceID) is not GameObject go) return;
            var styles = CharmStyles.Instance;
            if (styles == null) return;

            var own = styles.Find(Id(go));
            var tint = TintOf(go, styles, out var inherited);
            var iconRect = new Rect(rect.x, rect.y, 16f, rect.height);
            var styled = own != null && (own.color >= 0 || !string.IsNullOrEmpty(own.icon));

            if (styled)
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, EditorGUIUtility.currentViewWidth - rect.x, rect.height), Background(instanceID));

            if (tint >= 0)
            {
                var color = styles.colors[tint];
                color.a = inherited ? 0.18f : 0.4f;
                GUI.color = color;
                GUI.DrawTexture(new Rect(32f, rect.y, EditorGUIUtility.currentViewWidth - 32f, rect.height), Gradient);
                GUI.color = Color.white;
            }

            if (styled)
            {
                var texture = CharmStylePopup.Resolve(own.icon, styles);
                if (texture == null) texture = AssetPreview.GetMiniThumbnail(go);
                if (texture != null) GUI.DrawTexture(iconRect, texture, ScaleMode.ScaleToFit);

                label ??= new GUIStyle(EditorStyles.boldLabel);
                if (tint >= 0 && !inherited)
                {
                    var c = styles.colors[tint];
                    label.normal.textColor = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f > 0.65f ? new Color(0.1f, 0.1f, 0.1f) : Color.white;
                }
                else label.normal.textColor = new Color(0.824f, 0.824f, 0.824f);
                GUI.Label(new Rect(rect.x + 18f, rect.y, rect.width - 18f, rect.height), go.name, label);
            }

            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && iconRect.Contains(e.mousePosition))
            {
                PopupWindow.Show(iconRect, new CharmStylePopup(Id(go)));
                e.Use();
            }
        }

        static Color Background(int instanceID)
        {
            if (Selection.Contains(instanceID)) return new Color32(44, 93, 135, 255);
            return EditorGUIUtility.isProSkin ? new Color32(56, 56, 56, 255) : new Color32(200, 200, 200, 255);
        }

        static int TintOf(GameObject go, CharmStyles styles, out bool inherited)
        {
            inherited = false;
            for (var t = go.transform; t != null; t = t.parent)
            {
                var entry = styles.Find(Id(t.gameObject));
                if (entry == null || entry.color < 0 || entry.color >= styles.colors.Length) continue;
                inherited = t.gameObject != go;
                return entry.color;
            }
            return -1;
        }
    }

    sealed class CharmStylePopup : PopupWindowContent
    {
        const float Cell = 22f, Pad = 8f;
        const int Columns = 9;

        static readonly string[] Builtin =
        {
            "Folder Icon", "Prefab Icon", "GameObject Icon", "SceneAsset Icon", "AssemblyDefinitionAsset Icon", "Asset Store@2x", "cs Script Icon",
            "Camera Icon", "Light Icon", "Terrain Icon", "ParticleSystem Icon", "Rigidbody Icon", "BoxCollider Icon", "BlendDistance On@2x",
            "AudioSource Icon", "AudioClip Icon",
            "Animator Icon", "AvatarMask On Icon", "animationvisibilitytoggleon@2x", "BrushAttributes@2x", "AISparkle Icon",
            "Canvas Icon", "EventSystem Icon", "Favorite Icon",
        };

        readonly string id;
        string[] unity;
        Sprite[] sprites;
        Vector2 scroll;

        public CharmStylePopup(string id) => this.id = id;

        public override Vector2 GetWindowSize()
        {
            Warm(CharmStyles.Instance);
            return new Vector2(Pad * 2f + Cell * Columns, Mathf.Min(Pad * 2f + Height(CharmStyles.Instance), 360f));
        }

        float Height(CharmStyles styles)
        {
            var colors = styles != null ? styles.colors.Length : 0;
            return Rows(colors + 1) * Cell + 4f + Rows(1 + unity.Length + sprites.Length) * Cell;
        }

        static int Rows(int count) => count == 0 ? 0 : Mathf.CeilToInt(count / (float)Columns);

        public override void OnGUI(Rect rect)
        {
            var styles = CharmStyles.Instance;
            if (styles == null) return;
            Warm(styles);

            var content = Pad * 2f + Height(styles);
            var width = content > rect.height ? rect.width - 16f : rect.width;
            scroll = GUI.BeginScrollView(rect, scroll, new Rect(0f, 0f, width, content));

            var x = Pad;
            var y = Pad;
            var column = 0;

            if (Cross(new Rect(x, y, 18f, 18f))) Apply(styles, entry => entry.color = -1);
            Advance(ref x, ref y, ref column);
            for (var i = 0; i < styles.colors.Length; i++)
            {
                var swatch = new Rect(x, y, 18f, 18f);
                EditorGUI.DrawRect(swatch, styles.colors[i]);
                var pick = i;
                if (Click(swatch)) Apply(styles, entry => entry.color = pick);
                Advance(ref x, ref y, ref column);
            }

            if (column > 0) y += Cell;
            x = Pad;
            column = 0;
            y += 4f;

            if (Cross(new Rect(x, y, 18f, 18f))) Apply(styles, entry => entry.icon = null);
            Advance(ref x, ref y, ref column);
            foreach (var name in unity)
            {
                Slot(new Rect(x, y, 18f, 18f), EditorGUIUtility.IconContent(name).image, "u:" + name, styles);
                Advance(ref x, ref y, ref column);
            }
            foreach (var sprite in sprites)
            {
                Slot(new Rect(x, y, 18f, 18f), sprite.texture, sprite.name, styles);
                Advance(ref x, ref y, ref column);
            }

            GUI.EndScrollView();
        }

        void Slot(Rect rect, Texture texture, string key, CharmStyles styles)
        {
            if (texture != null) GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
            if (Click(rect)) Apply(styles, entry => entry.icon = key);
        }

        static void Advance(ref float x, ref float y, ref int column)
        {
            x += Cell;
            if (++column < Columns) return;
            column = 0;
            x = Pad;
            y += Cell;
        }

        internal static Texture Resolve(string key, CharmStyles styles)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (key.StartsWith("u:")) return EditorGUIUtility.IconContent(key[2..]).image;
            return styles.icons != null && styles.icons.TryGet(key, out var sprite) && sprite != null ? sprite.texture : null;
        }

        void Warm(CharmStyles styles)
        {
            if (unity == null)
            {
                var valid = new List<string>(Builtin.Length);
                foreach (var name in Builtin)
                {
                    var content = EditorGUIUtility.IconContent(name);
                    if (content != null && content.image != null) valid.Add(name);
                }
                unity = valid.ToArray();
            }

            if (sprites != null) return;
            if (styles == null || styles.icons == null) { sprites = Array.Empty<Sprite>(); return; }

            var items = new SerializedObject(styles.icons).FindProperty("items");
            var list = new List<Sprite>(items.arraySize);
            for (var i = 0; i < items.arraySize; i++)
                if (items.GetArrayElementAtIndex(i).objectReferenceValue is Sprite sprite) list.Add(sprite);
            sprites = list.ToArray();
        }

        void Apply(CharmStyles styles, Action<CharmStyles.Entry> change)
        {
            Undo.RecordObject(styles, "Charm Style");
            change(styles.Write(id));
            styles.Prune(id);
            EditorUtility.SetDirty(styles);
            EditorApplication.RepaintHierarchyWindow();
        }

        static bool Cross(Rect rect)
        {
            GUI.Label(rect, "×", EditorStyles.centeredGreyMiniLabel);
            return Click(rect);
        }

        static bool Click(Rect rect)
        {
            var e = Event.current;
            if (e.type != EventType.MouseDown || e.button != 0 || !rect.Contains(e.mousePosition)) return false;
            e.Use();
            return true;
        }
    }
}
