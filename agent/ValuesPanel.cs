using System;
using System.Collections.Generic;
using AllQuietInTheTrenches;
using UnityEngine;

namespace AQT
{
    // "Values" tab: global game values (resources, trust, counters...) with readable names,
    // grouped into collapsible sections, with favorites, Min/Max buttons and "Enter" to set a number.
    internal sealed class ValuesPanel
    {
        private sealed class Row
        {
            public string Name;
            public string Friendly;
            public string Search;       // names in both languages, lowercase
            public int Group;
            public SavedGameValue Saved;   // null = calculated value (read-only)
            public int Current;
            public bool HasMin, HasMax;
            public int Min, Max;
        }

        private sealed class GroupView
        {
            public string Id;
            public string Title;
            public List<Row> Rows = new List<Row>();
        }

        private readonly List<Row> _rows = new List<Row>();
        private readonly Dictionary<string, string> _edit = new Dictionary<string, string>();
        private readonly List<GroupView> _view = new List<GroupView>();
        private string _filter = "";
        private bool _showCalculated;
        private bool _vShowCalculated;
        private string _vFilter = "";
        private float _timer = 99f;

        private Ui _ui;
        private Settings _cfg;

        public string Status = "";

        public void Init(Ui ui, Settings cfg)
        {
            _ui = ui;
            _cfg = cfg;
        }

        // ---- data (called from Update, never from OnGUI) ------------------------------------------------

        public void Refresh(float dt, bool force)
        {
            _timer += dt;
            if (!force && _timer < 0.5f && _rows.Count > 0) return;
            _timer = 0f;

            _rows.Clear();
            GameValueManager gvm = Game.Find<GameValueManager>();
            if (gvm == null || gvm.MainGameValues == null) return;

            for (int i = 0; i < gvm.MainGameValues.Length; i++)
            {
                MainGameValue mv = gvm.MainGameValues[i];
                if (mv == null) continue;
                Row r = new Row();
                r.Name = mv.name;
                r.Saved = mv as SavedGameValue;
                try { r.Current = mv.GetValue(); }
                catch { continue; }
                r.Friendly = ValueNames.Friendly(r.Name);
                r.Search = ValueNames.AllNames(r.Name).ToLowerInvariant();
                r.Group = ValueNames.GroupOf(r.Name);
                if (r.Saved != null)
                {
                    r.HasMin = r.Saved.UseMinValue;
                    r.HasMax = r.Saved.UseMaxValue;
                    r.Min = r.Saved.MinValue;
                    r.Max = r.Saved.MaxValue;
                }
                _rows.Add(r);
            }
            _rows.Sort(delegate (Row a, Row b) { return string.Compare(a.Friendly, b.Friendly, StringComparison.OrdinalIgnoreCase); });
        }

        // ---- stable copy for IMGUI (taken on the Layout event) ----------------------------------------------

        private void Snapshot()
        {
            _vFilter = (_filter ?? "").Trim().ToLowerInvariant();
            _vShowCalculated = _showCalculated;

            _view.Clear();
            GroupView fav = new GroupView();
            fav.Id = "vfav";
            fav.Title = L.T("Favoritos", "Favorites");
            GroupView[] groups = new GroupView[ValueNames.GroupCount];
            for (int g = 0; g < groups.Length; g++)
            {
                groups[g] = new GroupView();
                groups[g].Id = ValueNames.GroupId(g);
                groups[g].Title = ValueNames.GroupTitle(g);
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                Row r = _rows[i];
                if (r.Saved == null && !_vShowCalculated) continue;
                if (_vFilter.Length > 0 && r.Search.IndexOf(_vFilter, StringComparison.Ordinal) < 0) continue;
                if (_cfg.FavValues.Contains(r.Name)) fav.Rows.Add(r);
                groups[r.Group].Rows.Add(r);
            }

            if (fav.Rows.Count > 0) _view.Add(fav);
            for (int g = 0; g < groups.Length; g++)
                if (groups[g].Rows.Count > 0) _view.Add(groups[g]);
        }

        // ---- interface ------------------------------------------------------------------------------------

        public void Draw()
        {
            if (Event.current.type == EventType.Layout) Snapshot();

            GUILayout.Label(L.T(
                "Valores globales de la partida (recursos, confianza, contadores...). Los editables tienen botones; pasá el mouse sobre un nombre para ver su nombre interno.",
                "Global game values (resources, trust, counters...). Editable ones have buttons; hover a name to see its internal name."));

            GUILayout.BeginHorizontal();
            GUILayout.Label(L.T("Buscar:", "Search:"), GUILayout.Width(Math.Max(55, _ui.Px * 5)));
            _filter = GUILayout.TextField(_filter);
            GUILayout.EndHorizontal();
            _showCalculated = GUILayout.Toggle(_showCalculated, L.T("Mostrar también los valores calculados (solo lectura)", "Also show calculated values (read-only)"), _ui.Toggle);

            if (_rows.Count == 0)
            {
                GUILayout.Label(L.T("No hay valores cargados todavía (abrí o cargá una partida).", "No values loaded yet (open or load a game)."));
                return;
            }

            if (_view.Count == 0)
            {
                GUILayout.Label(L.T("Ningún valor coincide con la búsqueda.", "No value matches the search."));
                return;
            }

            bool searching = _vFilter.Length > 0;
            for (int v = 0; v < _view.Count; v++)
            {
                GroupView gv = _view[v];
                string title = gv.Title + " (" + gv.Rows.Count + ")";
                if (!_ui.FoldHeader(_cfg, gv.Id, title, !searching)) continue;
                for (int i = 0; i < gv.Rows.Count; i++) DrawRow(gv.Rows[i]);
            }
        }

        private void DrawRow(Row r)
        {
            int w = Math.Max(1, _ui.Px);
            GUILayout.BeginHorizontal();

            bool isFav = _cfg.FavValues.Contains(r.Name);
            bool fav = _ui.FavToggle(isFav);
            if (fav != isFav) _cfg.SetListed(_cfg.FavValues, r.Name, fav);

            GUILayout.Label(new GUIContent(r.Friendly, r.Name), GUILayout.Width(w * 15));
            GUILayout.Label(r.Current.ToString(), GUILayout.Width(w * 4));

            if (r.Saved != null)
            {
                if (GUILayout.Button("-10", _ui.Small, GUILayout.Width(w * 3))) Change(r, r.Current - 10);
                if (GUILayout.Button("-1", _ui.Small, GUILayout.Width(w * 2.4f))) Change(r, r.Current - 1);
                if (GUILayout.Button("+1", _ui.Small, GUILayout.Width(w * 2.4f))) Change(r, r.Current + 1);
                if (GUILayout.Button("+10", _ui.Small, GUILayout.Width(w * 3))) Change(r, r.Current + 10);

                string txt;
                if (!_edit.TryGetValue(r.Name, out txt)) txt = "";
                string field = "val_" + r.Name;
                // "Enter" is detected BEFORE the TextField (the field may consume the event) and only if that field has focus.
                bool enter = Event.current.type == EventType.KeyDown &&
                             (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter) &&
                             GUI.GetNameOfFocusedControl() == field;
                GUI.SetNextControlName(field);
                txt = GUILayout.TextField(txt, GUILayout.Width(w * 4));
                _edit[r.Name] = txt;

                bool set = GUILayout.Button(L.T("Fijar", "Set"), _ui.Small, GUILayout.Width(w * 4.4f));
                if (enter) Event.current.Use();
                if (set || enter)
                {
                    int n;
                    if (int.TryParse(txt, out n)) Change(r, n);
                    else Status = L.T("Número inválido: ", "Invalid number: ") + txt;
                }

                GUI.enabled = r.HasMin;
                if (GUILayout.Button(L.T("Mín", "Min"), _ui.Small, GUILayout.Width(w * 3.4f))) Change(r, r.Min);
                GUI.enabled = r.HasMax;
                if (GUILayout.Button(L.T("Máx", "Max"), _ui.Small, GUILayout.Width(w * 3.4f))) Change(r, r.Max);
                GUI.enabled = true;
            }
            else
            {
                GUILayout.Label(L.T("(calculado)", "(calculated)"));
            }
            GUILayout.EndHorizontal();
        }

        private void Change(Row r, int newValue)
        {
            try
            {
                int applied = r.Saved.SetValue(newValue);
                r.Current = applied;
                Status = r.Friendly + " = " + applied;
                Log.Info("Value " + r.Name + " -> " + applied);
            }
            catch (Exception e)
            {
                Status = L.T("Error: ", "Error: ") + e.Message;
                Log.Error("ValuesPanel.Change", e);
            }
        }
    }
}
