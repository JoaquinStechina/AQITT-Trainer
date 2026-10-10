using System;
using System.Collections.Generic;
using AllQuietInTheTrenches;
using UnityEngine;

namespace AQT
{
    // "Characters" tab: add / remove attributes (Skills, Status, Circumstances, Relation) on the soldiers
    // the player controls (PlayerTroop: in troop + reserves), one at a time or the whole troop, with favorites and undo.
    internal class CharactersPanel
    {
        private class AttrRow
        {
            public CharacterAttribute Attr;
            public string Label;       // translated name (or the asset's name if it has none)
            public string Description;
            public string Category;    // "Skills", "Status", "Circumstances" or "Relation"
            public bool InGroup;       // belongs to a group (levels the game recalculates by itself)
            public Sprite Icon;
        }

        private sealed class UndoEntry
        {
            public string Text;
            public CharacterAttribute Attr;
            public bool Added;
            public List<Character> Chars = new List<Character>();
            public List<KeyValuePair<Character, CharacterAttribute>> OppositesRemoved = new List<KeyValuePair<Character, CharacterAttribute>>();
        }

        // Opposite pairs (they are not in a game group, so we make them mutually exclusive ourselves)
        private static readonly string[][] Opposites = new string[][]
        {
            new string[] { "Strong", "Weak" },
            new string[] { "Quick", "Slow" },
            new string[] { "Dextrous", "Clumsy" },
            new string[] { "KeenPerception", "PoorPerception" }
        };

        // Game attribute categories that can be edited (the asset name ends in "_<category>")
        private static readonly string[] Categories = new string[] { "Skills", "Status", "Circumstances", "Relation" };
        private const int FavoritesIndex = 4;   // "Favorites" tab (attributes from any category)

        private static string CategoryLabel(int i)
        {
            switch (i)
            {
                case 0: return L.T("Habilidades", "Skills");
                case 1: return L.T("Estado", "Status");
                case 2: return L.T("Circunst.", "Circumst.");
                case 3: return L.T("Relación", "Relation");
                default: return L.T("Favoritos", "Favorites");
            }
        }

        private readonly List<AttrRow> _all = new List<AttrRow>();
        private bool _built;

        private List<Character> _chars = new List<Character>();
        private string[] _charNames = new string[0];
        private int _selected;

        private HashSet<CharacterAttribute> _has = new HashSet<CharacterAttribute>();
        private int _cat;   // tab index (0..4)
        private string _filter = "";
        private bool _onlyOwned;
        private bool _allMode;          // apply to the whole troop instead of the selected soldier
        private float _timer = 99f;
        private Vector2 _charScroll;
        private AttributeManager _am;
        private bool _dirty;
        private readonly List<UndoEntry> _undo = new List<UndoEntry>();

        // IMGUI requires drawing EXACTLY the same controls on the Layout event and the following ones (MouseUp, Repaint...)
        // of the same pass; otherwise Unity throws "Getting control N's position in a group with only M controls".
        // So whatever decides which rows are shown is frozen on the Layout event (Snapshot) and the rest of the pass
        // uses that copy, even if the user changes something midway.
        private bool _vEmpty = true;
        private int _vCat;
        private int _vSelected;
        private Character _vChar;
        private string _vOwned = "";
        private string _vUndoText = "";
        private bool _vCanUndo;
        private bool _vIcons;
        private readonly List<AttrRow> _visible = new List<AttrRow>();
        private readonly List<bool> _visibleHas = new List<bool>();

        private Ui _ui;
        private Settings _cfg;

        public string Status = "";

        public void Init(Ui ui, Settings cfg)
        {
            _ui = ui;
            _cfg = cfg;
        }

        // ---- data -----------------------------------------------------------------------

        // Called from Update (never from OnGUI, so controls do not change mid-layout).
        public void Refresh(float dt, bool force)
        {
            _timer += dt;
            if (_dirty) { force = true; _dirty = false; }
            if (!force && _timer < 0.5f) return;
            _timer = 0f;

            CharacterManager cm = Game.Find<CharacterManager>();
            if (cm == null || cm.PlayerTroop == null) { _chars.Clear(); _charNames = new string[0]; return; }

            _am = cm.AttributeManager;
            if (!_built) BuildAttributeList(_am);

            Character keep = (_selected >= 0 && _selected < _chars.Count) ? _chars[_selected] : null;

            List<Character> list = cm.PlayerTroop.GetCharactersInTroop(CharacterStatus.InTroop, CharacterStatus.InReserves);
            _chars = list == null ? new List<Character>() : new List<Character>(list);
            _chars.Sort(delegate (Character a, Character b) { return string.Compare(NameOf(a), NameOf(b), StringComparison.OrdinalIgnoreCase); });

            _charNames = new string[_chars.Count];
            for (int i = 0; i < _chars.Count; i++) _charNames[i] = NameOf(_chars[i]);

            _selected = keep == null ? 0 : _chars.IndexOf(keep);
            if (_selected < 0) _selected = 0;

            _has.Clear();
            if (_chars.Count > 0 && _am != null)
            {
                List<CharacterAttribute> types = _am.GetCharacterAttributeTypes(_chars[_selected]);
                if (types != null)
                    for (int i = 0; i < types.Count; i++) _has.Add(types[i]);
            }
        }

        private static string NameOf(Character c)
        {
            if (c == null) return "?";
            string n = (c.Forename + " " + c.Surname).Trim();
            return n.Length > 0 ? n : c.name;
        }

        private void BuildAttributeList(AttributeManager am)
        {
            _all.Clear();
            if (am == null || am.CharacterAttributes == null) return;
            for (int i = 0; i < am.CharacterAttributes.Length; i++)
            {
                CharacterAttribute a = am.CharacterAttributes[i];
                if (a == null || a.Category == null) continue;

                string cat = a.Category.name;           // e.g. AttributeCategory_4_Skills
                string kind = null;
                for (int k = 0; k < Categories.Length; k++)
                {
                    if (cat.EndsWith("_" + Categories[k], StringComparison.OrdinalIgnoreCase)) { kind = Categories[k]; break; }
                }
                if (kind == null) continue;
                if (a.Icon == null) continue;           // no icon = internal, the game does not show it

                AttrRow r = new AttrRow();
                r.Attr = a;
                r.Category = kind;
                r.Icon = a.Icon;
                string h = "";
                try { h = a.GetHeader(); } catch (Exception) { }
                if (h == null || h.Trim().Length == 0 || h.Trim() == "?") h = a.name;
                r.Label = h;
                try { r.Description = a.GetDescription(); } catch (Exception) { r.Description = ""; }
                r.InGroup = am.GetCharacterAttributeGroup(a) != null;
                _all.Add(r);
            }
            _all.Sort(delegate (AttrRow x, AttrRow y)
            {
                int c = string.Compare(x.Category, y.Category, StringComparison.Ordinal);
                return c != 0 ? c : string.Compare(x.Label, y.Label, StringComparison.OrdinalIgnoreCase);
            });
            _built = true;
        }

        // ---- stable copy for IMGUI (taken on the Layout event) -----------------------------------

        private void Snapshot()
        {
            _vEmpty = _chars.Count == 0;
            _vCat = _cat;
            _vIcons = _cfg.ShowIcons;
            _vSelected = _vEmpty ? 0 : Math.Max(0, Math.Min(_selected, _chars.Count - 1));
            _vChar = _vEmpty ? null : _chars[_vSelected];

            _vCanUndo = _undo.Count > 0;
            _vUndoText = _vCanUndo ? _undo[_undo.Count - 1].Text : "";

            _visible.Clear();
            _visibleHas.Clear();
            _vOwned = "";
            if (_vEmpty) return;

            string want = _vCat < Categories.Length ? Categories[_vCat] : null;
            string f = _filter == null ? "" : _filter.Trim();
            List<string> owned = new List<string>();
            for (int i = 0; i < _all.Count; i++)
            {
                AttrRow r = _all[i];
                bool has = _has.Contains(r.Attr);
                if (has) owned.Add(r.Label);

                if (want != null) { if (r.Category != want) continue; }
                else if (!_cfg.FavAttrs.Contains(r.Attr.name)) continue;

                if (_onlyOwned && !has) continue;
                if (f.Length > 0 &&
                    r.Label.IndexOf(f, StringComparison.OrdinalIgnoreCase) < 0 &&
                    r.Attr.name.IndexOf(f, StringComparison.OrdinalIgnoreCase) < 0) continue;
                _visible.Add(r);
                _visibleHas.Add(has);
            }
            _vOwned = owned.Count == 0 ? "-" : string.Join(", ", owned.ToArray());
        }

        // ---- interface ---------------------------------------------------------------------

        public void Draw()
        {
            // Recomputed only on Layout; Repaint / MouseUp / KeyDown of that pass reuse the same copy.
            if (Event.current.type == EventType.Layout) Snapshot();

            if (_vEmpty)
            {
                GUILayout.Label(L.T("No hay personajes del jugador cargados todavía (abrí o cargá una partida).",
                                    "No player characters loaded yet (open or load a game)."));
                return;
            }

            GUILayout.Label(L.T("Personaje:", "Character:"));
            _charScroll = GUILayout.BeginScrollView(_charScroll, GUILayout.Height(Math.Max(90, _ui.Px * 8)));
            int sel = GUILayout.SelectionGrid(_vSelected, _charNames, 2);
            GUILayout.EndScrollView();
            if (sel != _vSelected) { _selected = sel; _dirty = true; }

            GUILayout.BeginHorizontal();
            for (int k = 0; k <= FavoritesIndex; k++)
            {
                if (GUILayout.Toggle(_cat == k, CategoryLabel(k), "Button")) _cat = k;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            _onlyOwned = GUILayout.Toggle(_onlyOwned, L.T("Solo los que tiene", "Only the ones it has"));
            _allMode = GUILayout.Toggle(_allMode, L.T("Aplicar a TODA la tropa (", "Apply to the WHOLE troop (") + _chars.Count + ")");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(L.T("Buscar:", "Search:"), GUILayout.Width(Math.Max(55, _ui.Px * 5)));
            _filter = GUILayout.TextField(_filter);
            GUILayout.EndHorizontal();

            GUILayout.Label(L.T("Tiene: ", "Has: ") + _vOwned, _ui.Hint);

            GUILayout.BeginHorizontal();
            GUI.enabled = _vCanUndo;
            if (GUILayout.Button(_vCanUndo ? L.T("Deshacer: ", "Undo: ") + _vUndoText : L.T("Deshacer (nada para deshacer)", "Undo (nothing to undo)")))
                Undo();
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            if (_vCat != 0)
                GUILayout.Label(L.T("Ojo: los marcados (auto) son niveles que el juego recalcula solo; para cambiarlos de verdad usá la pestaña Trucos / Valores.",
                                    "Note: items marked (auto) are levels the game recalculates by itself; to really change them use the Cheats / Values tab."),
                                _ui.Hint);
            if (_vCat == FavoritesIndex && _visible.Count == 0)
                GUILayout.Label(L.T("Todavía no marcaste favoritos: tocá \"Fav\" en cualquier atributo.", "No favorites yet: press \"Fav\" on any attribute."), _ui.Hint);

            AttributeManager am = _am;
            Character c = _vChar;
            int px = Math.Max(1, _ui.Px);

            for (int i = 0; i < _visible.Count; i++)
            {
                AttrRow r = _visible[i];
                bool has = _visibleHas[i];

                GUILayout.BeginHorizontal();

                // game icon (the space is always reserved; it is only drawn on Repaint)
                Rect ir = GUILayoutUtility.GetRect(px * 1.7f, px * 1.7f, GUILayout.Width(px * 1.7f), GUILayout.Height(px * 1.7f));
                if (_vIcons && Event.current.type == EventType.Repaint) DrawSprite(ir, r.Icon);

                bool isFav = _cfg.FavAttrs.Contains(r.Attr.name);
                bool fav = _ui.FavToggle(isFav);
                if (fav != isFav) _cfg.SetListed(_cfg.FavAttrs, r.Attr.name, fav);

                Color old = GUI.contentColor;
                if (has) GUI.contentColor = Ui.Green;
                GUILayout.Label(new GUIContent(r.Label + (r.InGroup ? "  (auto)" : ""), r.Description), GUILayout.Width(px * 15));
                GUI.contentColor = old;

                GUI.enabled = !has || _allMode;
                if (GUILayout.Button(L.T("Agregar", "Add"), GUILayout.Width(px * 7))) ApplyAttribute(r, true);
                GUI.enabled = has || _allMode;
                if (GUILayout.Button(L.T("Quitar", "Remove"), GUILayout.Width(px * 7))) ApplyAttribute(r, false);
                GUI.enabled = true;

                GUILayout.EndHorizontal();
            }
            // The description (tooltip) is shown in a fixed label below the list (TrainerBehaviour.DrawWindow):
            // adding it only on hover changed the number of controls.
        }

        // Draws a Sprite inside a rectangle keeping its aspect ratio.
        private static void DrawSprite(Rect r, Sprite s)
        {
            try
            {
                if (s == null) return;
                if (s.packed && s.packingMode == SpritePackingMode.Tight) return;   // the texture rectangle is not usable with "tight" packing
                Texture2D t = s.texture;
                if (t == null) return;
                Rect tr = s.textureRect;
                if (tr.width <= 0f || tr.height <= 0f) return;
                Rect uv = new Rect(tr.x / t.width, tr.y / t.height, tr.width / t.width, tr.height / t.height);
                float aspect = tr.width / tr.height;
                Rect d = r;
                if (aspect > 1f) { d.height = r.width / aspect; d.y += (r.height - d.height) * 0.5f; }
                else { d.width = r.height * aspect; d.x += (r.width - d.width) * 0.5f; }
                GUI.DrawTextureWithTexCoords(d, t, uv, true);
            }
            catch (Exception)
            {
                // an icon that cannot be drawn is simply skipped
            }
        }

        // ---- actions --------------------------------------------------------------------

        private void ApplyAttribute(AttrRow r, bool add)
        {
            try
            {
                AttributeManager am = _am;
                List<Character> targets = new List<Character>();
                if (_allMode) targets.AddRange(_chars);
                else if (_vChar != null) targets.Add(_vChar);
                if (am == null || targets.Count == 0) return;

                UndoEntry u = new UndoEntry();
                u.Attr = r.Attr;
                u.Added = add;
                string opp = add ? OppositeOf(r.Attr.name) : null;
                CharacterAttribute oa = opp != null ? am.GetCharacterAttributeType(opp) : null;

                for (int i = 0; i < targets.Count; i++)
                {
                    Character c = targets[i];
                    if (add)
                    {
                        // remove the opposite if it exists (Strong <-> Weak, etc.)
                        if (oa != null && Game.RemoveAttribute(am, c, oa) > 0)
                            u.OppositesRemoved.Add(new KeyValuePair<Character, CharacterAttribute>(c, oa));
                        if (Game.AddAttribute(am, c, r.Attr)) u.Chars.Add(c);
                    }
                    else
                    {
                        if (Game.RemoveAttribute(am, c, r.Attr) > 0) u.Chars.Add(c);
                    }
                }

                string verb = add ? L.T("agregado", "added") : L.T("quitado", "removed");
                if (targets.Count == 1)
                {
                    string who = NameOf(targets[0]);
                    Status = u.Chars.Count > 0
                        ? who + ": " + verb + " '" + r.Label + "'"
                        : who + (add ? L.T(" ya tiene '", " already has '") : L.T(" no tiene '", " does not have '")) + r.Label + "'";
                }
                else
                {
                    Status = "'" + r.Label + "' " + verb + L.T(" en ", " on ") + u.Chars.Count + "/" + targets.Count + L.T(" soldados", " soldiers");
                }
                Log.Info(Status);

                if (u.Chars.Count > 0)
                {
                    u.Text = (add ? "+ " : "- ") + r.Label + (u.Chars.Count > 1 ? " (x" + u.Chars.Count + ")" : "");
                    _undo.Add(u);
                    if (_undo.Count > 30) _undo.RemoveAt(0);
                }
            }
            catch (Exception e)
            {
                Status = "Error: " + e.Message + " (" + Log.Path + ")";
                Log.Error("CharactersPanel.ApplyAttribute", e);
            }
            _dirty = true;
        }

        private void Undo()
        {
            if (_undo.Count == 0 || _am == null) return;
            UndoEntry u = _undo[_undo.Count - 1];
            _undo.RemoveAt(_undo.Count - 1);
            try
            {
                for (int i = 0; i < u.Chars.Count; i++)
                {
                    Character c = u.Chars[i];
                    if (c == null) continue;
                    if (u.Added) Game.RemoveAttribute(_am, c, u.Attr);
                    else Game.AddAttribute(_am, c, u.Attr);
                }
                if (u.Added)
                {
                    for (int i = 0; i < u.OppositesRemoved.Count; i++)
                    {
                        Character c = u.OppositesRemoved[i].Key;
                        if (c != null) Game.AddAttribute(_am, c, u.OppositesRemoved[i].Value);
                    }
                }
                Status = L.T("Deshecho: ", "Undone: ") + u.Text;
                Log.Info(Status);
            }
            catch (Exception e)
            {
                Status = "Error: " + e.Message + " (" + Log.Path + ")";
                Log.Error("CharactersPanel.Undo", e);
            }
            _dirty = true;
        }

        private static string OppositeOf(string name)
        {
            for (int i = 0; i < Opposites.Length; i++)
            {
                if (Opposites[i][0] == name) return Opposites[i][1];
                if (Opposites[i][1] == name) return Opposites[i][0];
            }
            return null;
        }
    }
}
