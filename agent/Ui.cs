using System;
using System.Collections.Generic;
using UnityEngine;

namespace AQT
{
    // UI utilities (IMGUI): own skin with adjustable font size (does not touch the game's skin),
    // shared styles, collapsible sections and the "Fav" toggle.
    //
    // IMGUI RULE followed by the whole menu: within one pass (Layout -> event -> Repaint) exactly
    // the same controls must be drawn. So whatever decides how many controls exist (collapsed sections, search boxes, filters, tab)
    // is copied on the Layout event and the rest of the pass uses that copy.
    internal sealed class Ui
    {
        public static readonly Color Green = new Color(0.55f, 1f, 0.55f);
        public static readonly Color Gold = new Color(1f, 0.85f, 0.25f);
        public static readonly Color Gray = new Color(0.62f, 0.62f, 0.62f);

        public GUIStyle Toggle, Desc, Header, Hint, Fold, Small, Hud;
        public int Px = 13;

        private GUISkin _skin;
        private GUISkin _prevSkin;
        private Color _prevColor, _prevContent, _prevBackground;
        private bool _prevEnabled;
        private readonly HashSet<string> _vCollapsed = new HashSet<string>();

        // Called at the start of every OnGUI: sets the own skin and the opacity.
        public void Begin(Settings cfg)
        {
            _prevSkin = GUI.skin;
            _prevColor = GUI.color;
            _prevContent = GUI.contentColor;
            _prevBackground = GUI.backgroundColor;
            _prevEnabled = GUI.enabled;

            int px = Mathf.Clamp(Mathf.RoundToInt(13f * cfg.FontScale), 9, 28);
            if (_skin == null || px != Px || Toggle == null) Build(px);

            GUI.skin = _skin;
            GUI.color = new Color(1f, 1f, 1f, cfg.Alpha);
            GUI.contentColor = Color.white;
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;
        }

        public void End()
        {
            if (_prevSkin != null) GUI.skin = _prevSkin;
            GUI.color = _prevColor;
            GUI.contentColor = _prevContent;
            GUI.backgroundColor = _prevBackground;
            GUI.enabled = _prevEnabled;
        }

        private void Build(int px)
        {
            Px = px;
            GUISkin src = _prevSkin != null ? _prevSkin : GUI.skin;
            GUISkin old = _skin;
            GUISkin skin = null;
            try { skin = UnityEngine.Object.Instantiate(src); }
            catch (Exception e) { Log.Error("Ui.Build (Instantiate skin)", e); }

            if (skin != null)
            {
                skin.hideFlags = HideFlags.HideAndDontSave;
                skin.label.fontSize = px;
                skin.toggle.fontSize = px;
                skin.button.fontSize = px;
                skin.textField.fontSize = px;
                skin.textArea.fontSize = px;
                skin.box.fontSize = px;
                skin.window.fontSize = px;
                _skin = skin;
            }
            else
            {
                _skin = src;   // no copy: the game's skin is used (the font size cannot be changed)
            }
            if (old != null && old != _skin && old != src) UnityEngine.Object.Destroy(old);

            Toggle = new GUIStyle(_skin.toggle);
            Toggle.wordWrap = true;

            Desc = new GUIStyle(_skin.label);
            Desc.wordWrap = true;
            Desc.fontSize = Math.Max(9, px - 2);
            Desc.padding.left = 22;
            Desc.margin.top = 0;
            Desc.margin.bottom = 4;
            Desc.normal.textColor = Gray;

            Hint = new GUIStyle(_skin.label);
            Hint.wordWrap = true;
            Hint.fontSize = Math.Max(9, px - 2);
            Hint.normal.textColor = Gray;

            Header = new GUIStyle(_skin.label);
            Header.fontStyle = FontStyle.Bold;
            Header.margin.top = 6;

            Fold = new GUIStyle(_skin.button);
            Fold.alignment = TextAnchor.MiddleLeft;
            Fold.fontStyle = FontStyle.Bold;

            Small = new GUIStyle(_skin.button);
            Small.fontSize = Math.Max(9, px - 1);
            Small.padding = new RectOffset(2, 2, 2, 2);

            Hud = new GUIStyle(_skin.box);
            Hud.fontSize = Math.Max(9, px - 1);
            Hud.alignment = TextAnchor.MiddleLeft;
            Hud.wordWrap = false;
        }

        // ------------------------------------------------------------------ collapsible sections

        public void SnapshotCollapsed(Settings cfg)
        {
            _vCollapsed.Clear();
            foreach (string s in cfg.Collapsed) _vCollapsed.Add(s);
        }

        public bool IsCollapsed(string id)
        {
            return _vCollapsed.Contains(id);
        }

        // Clickable header. Returns true if the section must be shown expanded (according to the Layout copy).
        public bool FoldHeader(Settings cfg, string id, string title, bool allowCollapse)
        {
            bool collapsed = allowCollapse && _vCollapsed.Contains(id);
            if (GUILayout.Button((collapsed ? "[+]  " : "[-]  ") + title, Fold) && allowCollapse)
            {
                if (cfg.Collapsed.Contains(id)) cfg.Collapsed.Remove(id); else cfg.Collapsed.Add(id);
                cfg.Touch();
            }
            return !collapsed;
        }

        // ------------------------------------------------------------------ "Fav" toggle

        public bool FavToggle(bool on)
        {
            Color c = GUI.contentColor;
            if (on) GUI.contentColor = Gold;
            bool r = GUILayout.Toggle(on, "Fav", Small, GUILayout.Width(Math.Max(34, Px * 3)));
            GUI.contentColor = c;
            return r;
        }
    }
}
