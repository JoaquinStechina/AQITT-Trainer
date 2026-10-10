using System;
using System.Collections.Generic;
using AllQuietInTheTrenches;
using UnityEngine;

namespace AQT
{
    public class TrainerBehaviour : MonoBehaviour
    {
        private sealed class SectionView
        {
            public string Id;
            public string Title;
            public bool IsFav;
            public List<Cheat> Items = new List<Cheat>();
        }

        private const int WindowId = 0x41515;
        private const float TickSeconds = 0.3f;
        private const int TabCount = 4;

        private Settings _cfg;
        private Ui _ui;
        private readonly ValuesPanel _values = new ValuesPanel();
        private readonly CharactersPanel _characters = new CharactersPanel();
        private readonly Wellbeing _wellbeing = new Wellbeing();
        private readonly SuccessBoost _boost = new SuccessBoost();

        private bool _show = true;
        private Rect _window = new Rect(20, 20, 620, 700);
        private float _winW = 620f, _winH = 700f;
        private int _tab;
        private readonly Vector2[] _scrolls = new Vector2[TabCount];
        private string _status = "";
        private string _lastError = "";

        // estado de los trucos
        private bool[] _prevOn;
        private bool _appliedInvWound, _appliedInvSurrender;
        private float _timer;
        private float _scopeTimer = 99f;
        private bool _inMission;
        private int _scope;
        private bool _langDirty;
        private string _search = "";

        // copia estable para IMGUI (se toma en el evento Layout de cada pasada; ver Ui.cs)
        private int _drawTab;
        private string _vSearch = "";
        private readonly List<SectionView> _sections = new List<SectionView>();

        // arrastre del borde para cambiar el tamano de la ventana
        private Vector2 _grab;

        // ------------------------------------------------------------------ ciclo de vida

        private void Awake()
        {
            try
            {
                _cfg = Settings.Load();
                L.Lang = Mathf.Clamp(_cfg.Language, 0, L.Count - 1);
                _ui = new Ui();
                _values.Init(_ui, _cfg);
                _characters.Init(_ui, _cfg);

                // trucos que estaban activos la ultima vez
                if (_cfg.RememberCheats) Cheats.SetActive(_cfg.ActiveCheats);
                else Cheats.SetActive(new string[0]);
                _prevOn = new bool[Cheats.All.Count];
                for (int i = 0; i < _prevOn.Length; i++) _prevOn[i] = Cheats.All[i].On;

                _tab = Mathf.Clamp(_cfg.Tab, 0, TabCount - 1);
                _winW = Mathf.Clamp(_cfg.WinW, 460f, Mathf.Max(460f, Screen.width));
                _winH = Mathf.Clamp(_cfg.WinH, 380f, Mathf.Max(380f, Screen.height - 20f));
                float x = _cfg.WinX < 0f ? 20f : _cfg.WinX;
                float y = _cfg.WinY < 0f ? 20f : _cfg.WinY;
                _window = new Rect(x, y, _winW, _winH);
                ClampWindow();

                _status = L.T("Listo. Abrí una partida y activá lo que quieras.", "Ready. Open a game and turn on what you want.");
                if (Cheats.ActiveCount() > 0)
                    _status = L.T("Se reactivaron ", "Re-enabled ") + Cheats.ActiveCount() + L.T(" trucos de la sesión anterior.", " cheats from the last session.");
            }
            catch (Exception e)
            {
                Log.Error("Awake", e);
                if (_cfg == null) _cfg = new Settings();
                if (_ui == null) _ui = new Ui();
                if (_prevOn == null) _prevOn = new bool[Cheats.All.Count];
            }
        }

        private void Update()
        {
            try
            {
                if (Input.GetKeyDown(KeyCode.F10) || Input.GetKeyDown(KeyCode.Insert))
                    _show = !_show;

                float dt = Time.unscaledDeltaTime;

                HandleHotkeys();
                SyncCheats(false);

                _scopeTimer += dt;
                if (_scopeTimer >= 1f) RefreshScope();

                _timer += dt;
                if (_timer >= TickSeconds)
                {
                    _timer = 0f;
                    Tick();
                }

                if (_langDirty)
                {
                    _langDirty = false;
                    _values.Refresh(0f, true);
                }

                if (_show)
                {
                    if (_tab == 1) _values.Refresh(dt, false);
                    if (_tab == 2) _characters.Refresh(dt, false);
                }

                if (!string.IsNullOrEmpty(_values.Status)) { _status = _values.Status; _values.Status = ""; }
                if (!string.IsNullOrEmpty(_characters.Status)) { _status = _characters.Status; _characters.Status = ""; }

                SyncWindowSettings();
                _cfg.SaveIfDue();
            }
            catch (Exception e)
            {
                ReportError("Update", e);
            }
        }

        private void OnDestroy()
        {
            try { RestoreAttributes(); } catch (Exception e) { Log.Error("OnDestroy", e); }
            try { if (_cfg != null && _cfg.Dirty) _cfg.Save(); } catch (Exception e) { Log.Error("OnDestroy (ajustes)", e); }
        }

        private void ReportError(string where, Exception e)
        {
            string key = where + e.GetType().Name + e.Message;
            if (key != _lastError)
            {
                _lastError = key;
                Log.Error(where, e);
                _status = "Error: " + where + " - " + e.Message + " (" + Log.Path + ")";
            }
        }

        // ------------------------------------------------------------------ trucos: estado

        private static bool On(string id)
        {
            Cheat c = Cheats.Get(id);
            return c != null && c.On;
        }

        // Detecta cambios en las casillas (desde el menu, un perfil o una tecla rapida), los guarda en los ajustes
        // y, si cambio uno solo, avisa que paso y a cuantos soldados alcanza.
        private void SyncCheats(bool quiet)
        {
            if (_prevOn == null || _prevOn.Length != Cheats.All.Count) return;
            int changed = 0;
            Cheat last = null;
            for (int i = 0; i < Cheats.All.Count; i++)
            {
                Cheat c = Cheats.All[i];
                if (c.On != _prevOn[i]) { _prevOn[i] = c.On; changed++; last = c; }
            }
            if (changed == 0) return;

            _cfg.ActiveCheats = Cheats.ActiveIds();
            _cfg.Touch();

            if (!quiet && changed == 1 && last != null)
            {
                RefreshScope();
                string msg = last.Title + (last.On ? L.T(": activado", ": on") : L.T(": desactivado", ": off"));
                if (last.On)
                {
                    if (last.MissionOnly && !_inMission)
                        msg += L.T("  (todavía no hay misión: actúa cuando empiece una)", "  (no mission yet: it acts once one starts)");
                    else if (last.Id != "relGroups")
                        msg += L.T("  - alcance: ", "  - scope: ") + _scope + L.T(" soldados", " soldiers");
                }
                _status = msg;
                Log.Info(msg);
            }
        }

        private void RefreshScope()
        {
            _scopeTimer = 0f;
            CharacterManager cm = Game.Find<CharacterManager>();
            MissionManager mm = Game.Find<MissionManager>();
            _inMission = mm != null && mm.IsEnabled;
            _scope = cm == null ? 0 : Game.MyCharacters(cm, mm).Count;
        }

        // Alt + 1..9 activan / desactivan los trucos marcados como favoritos (en el orden en que se marcaron).
        private void HandleHotkeys()
        {
            if (!_cfg.Hotkeys || _cfg.FavCheats.Count == 0) return;
            if (!(Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))) return;
            for (int k = 0; k < 9 && k < _cfg.FavCheats.Count; k++)
            {
                if (!Input.GetKeyDown(KeyCode.Alpha1 + k)) continue;
                Cheat c = Cheats.Get(_cfg.FavCheats[k]);
                if (c != null) c.On = !c.On;
            }
        }

        private void ApplyPreset(Preset p)
        {
            Cheats.SetActive(p.Ids);
            SyncCheats(true);
            _status = L.T("Perfil \"", "Profile \"") + p.Name + L.T("\" aplicado: ", "\" applied: ") + Cheats.ActiveCount() +
                      L.T(" trucos activos.", " cheats active.");
            Log.Info(_status);
        }

        private void SaveProfile(int k)
        {
            _cfg.Profiles[k] = Cheats.ActiveIds();
            _cfg.ProfileSet[k] = true;
            _cfg.Touch();
            _status = L.T("Perfil ", "Profile ") + (k + 1) + L.T(" guardado (", " saved (") + _cfg.Profiles[k].Count + L.T(" trucos).", " cheats).");
        }

        private void LoadProfile(int k)
        {
            if (!_cfg.ProfileSet[k]) return;
            Cheats.SetActive(_cfg.Profiles[k]);
            SyncCheats(true);
            _status = L.T("Perfil ", "Profile ") + (k + 1) + L.T(" cargado: ", " loaded: ") + Cheats.ActiveCount() + L.T(" trucos activos.", " cheats active.");
        }

        // ------------------------------------------------------------------ trucos: aplicacion

        // Ejecuta un truco aislado: si uno falla, los demas siguen funcionando y el error queda en el log.
        private void Safe(string name, Action action)
        {
            try { action(); }
            catch (Exception e) { ReportError(name, e); }
        }

        private void Tick()
        {
            bool invWound = On("invWound");
            bool invSurrender = On("invSurrender");
            bool success = On("success");

            bool needAttrCleanup = (!invWound && _appliedInvWound) || (!invSurrender && _appliedInvSurrender) ||
                                   (!success && _boost.Active);
            if (Cheats.ActiveCount() == 0 && !needAttrCleanup) return;

            CharacterManager cm = Game.Find<CharacterManager>();
            MissionManager mm = Game.Find<MissionManager>();
            if (cm == null || mm == null) return;

            List<Character> chars = Game.MyCharacters(cm, mm);
            if (chars.Count == 0) return;

            if (On("ammo")) Game.TopUp(mm.EquipmentAmmunitionValue, chars);
            if (On("firstAid")) Game.TopUp(mm.EquipmentFirstAidValue, chars);
            if (On("handGrenades")) Game.TopUp(mm.EquipmentHandGrenadesValue, chars);
            if (On("smokeGrenades")) Game.TopUp(mm.EquipmentSmokeGrenadesValue, chars);

            if (On("noStress")) Game.ToBest(mm.StressGameValue, chars, false, 0);
            if (On("noTired"))
            {
                Game.ToBest(mm.PhysicalExertionGameValue, chars, false, 0);   // de la mision (el juego lo reinicia en cada mision)
                Game.ToBest(mm.ExhaustionGameValue, chars, false, 0);         // de la campana (se acumula entre misiones)
            }

            AttributeManager am = cm.AttributeManager;
            if (invWound) { Game.EnsureAttribute(am, chars, mm.InvincibleWound, true); _appliedInvWound = true; }
            else if (_appliedInvWound) { Game.EnsureAttribute(am, chars, mm.InvincibleWound, false); _appliedInvWound = false; }

            if (invSurrender) { Game.EnsureAttribute(am, chars, mm.InvincibleSurrender, true); _appliedInvSurrender = true; }
            else if (_appliedInvSurrender) { Game.EnsureAttribute(am, chars, mm.InvincibleSurrender, false); _appliedInvSurrender = false; }

            // Acciones siempre exitosas (eficiencias de combate / primeros auxilios / saqueo...)
            if (success) Safe("Acciones exitosas", delegate { _boost.Apply(mm, chars); });
            else if (_boost.Active) Safe("Acciones exitosas (revertir)", delegate { _boost.Revert(); });

            // Bienestar (hambre / animo / salud / fervor / forma fisica / circunstancias / relaciones)
            bool noHunger = On("noHunger"), happy = On("happy"), health = On("health"), fervor = On("fervor"), fitness = On("fitness");
            bool circ = On("circ"), relPlayer = On("relPlayer"), relGroups = On("relGroups"), relFriends = On("relFriends");
            if (noHunger || happy || health || fervor || fitness || circ || relPlayer || relGroups || relFriends)
            {
                MissionImpactManager mi = Game.Find<MissionImpactManager>();
                GameValueManager gvm = Game.Find<GameValueManager>();
                bool ready = false;
                Safe("Bienestar", delegate { ready = _wellbeing.Bind(gvm, am, mi, mm); });
                if (ready)
                {
                    if (noHunger) Safe("Sin hambre", delegate { _wellbeing.NoHunger(chars); });
                    if (happy) Safe("Felices", delegate { _wellbeing.Happy(chars, mm); });
                    if (health) Safe("Buena salud", delegate { _wellbeing.Health(chars, mm); });
                    if (fervor) Safe("Exaltados", delegate { _wellbeing.Fervor(chars, mm); });
                    if (fitness) Safe("Condicion fisica", delegate { _wellbeing.Fitness(chars, mm); });
                    if (circ) Safe("Circunstancias", delegate { _wellbeing.Circumstances(chars); });
                    if (relPlayer) Safe("Relaciones (soldados)", delegate { _wellbeing.Relations(chars); });
                    if (relGroups) Safe("Relaciones (hospital/intendente/aldea)", delegate { _wellbeing.GroupTrust(); });
                    if (relFriends) Safe("Relaciones (amistades)", delegate { _wellbeing.Friends(chars, cm); });
                }
            }
        }

        // Quita los atributos de invencibilidad (al descargar el trainer) para no dejarlos en la partida.
        private void RestoreAttributes()
        {
            // Devuelve las eficiencias que se sumaron para las acciones exitosas
            try { _boost.Revert(); } catch (Exception e) { Log.Error("Revert acciones", e); }

            if (!_appliedInvWound && !_appliedInvSurrender) return;
            CharacterManager cm = Game.Find<CharacterManager>();
            MissionManager mm = Game.Find<MissionManager>();
            if (cm == null || mm == null) return;
            List<Character> chars = Game.MyCharacters(cm, mm);
            if (_appliedInvWound) Game.EnsureAttribute(cm.AttributeManager, chars, mm.InvincibleWound, false);
            if (_appliedInvSurrender) Game.EnsureAttribute(cm.AttributeManager, chars, mm.InvincibleSurrender, false);
            _appliedInvWound = false;
            _appliedInvSurrender = false;
        }

        // ------------------------------------------------------------------ ventana: posicion / tamano

        private void ClampWindow()
        {
            float sw = Mathf.Max(200f, Screen.width);
            float sh = Mathf.Max(200f, Screen.height);
            _window.x = Mathf.Clamp(_window.x, -_winW + 80f, sw - 80f);
            _window.y = Mathf.Clamp(_window.y, 0f, sh - 30f);
        }

        // Guarda posicion, tamano y pestana cuando cambian (el archivo se escribe con retraso, ver Settings.SaveIfDue).
        private void SyncWindowSettings()
        {
            bool changed = false;
            if (Mathf.Abs(_cfg.WinX - _window.x) > 0.5f) { _cfg.WinX = _window.x; changed = true; }
            if (Mathf.Abs(_cfg.WinY - _window.y) > 0.5f) { _cfg.WinY = _window.y; changed = true; }
            if (Mathf.Abs(_cfg.WinW - _winW) > 0.5f) { _cfg.WinW = _winW; changed = true; }
            if (Mathf.Abs(_cfg.WinH - _winH) > 0.5f) { _cfg.WinH = _winH; changed = true; }
            if (_cfg.Tab != _tab) { _cfg.Tab = _tab; changed = true; }
            if (changed) _cfg.Touch();
        }

        // ------------------------------------------------------------------ interfaz (IMGUI)

        private void OnGUI()
        {
            if (_ui == null || _cfg == null) return;
            try
            {
                _ui.Begin(_cfg);
                try
                {
                    if (!_show)
                    {
                        if (_cfg.ShowHud) DrawHud();
                        return;
                    }
                    ClampWindow();
                    _window = GUILayout.Window(WindowId, _window, DrawWindow,
                        "All Quiet in the Trenches - Trainer  [F10 / Insert]",
                        GUILayout.Width(_winW), GUILayout.Height(_winH));
                }
                finally
                {
                    _ui.End();
                }
            }
            catch (Exception e)
            {
                ReportError("OnGUI", e);
            }
        }

        // Indicador chico para saber que el trainer sigue activo con el menu oculto.
        private void DrawHud()
        {
            if (Event.current.type != EventType.Repaint) return;
            int n = Cheats.ActiveCount();
            string text = "AQT: " + n + L.T(" activos", " active") + "   [F10]";
            Vector2 size = _ui.Hud.CalcSize(new GUIContent(text));
            GUI.Box(new Rect(8f, 8f, size.x + 14f, size.y + 6f), text, _ui.Hud);
        }

        private void TakeSnapshot()
        {
            _drawTab = _tab;
            _ui.SnapshotCollapsed(_cfg);
            _vSearch = (_search ?? "").Trim().ToLowerInvariant();

            _sections.Clear();
            if (_vSearch.Length == 0)
            {
                SectionView fav = new SectionView();
                fav.Id = "c_fav";
                fav.Title = L.T("Favoritos", "Favorites");
                fav.IsFav = true;
                for (int i = 0; i < _cfg.FavCheats.Count; i++)
                {
                    Cheat c = Cheats.Get(_cfg.FavCheats[i]);
                    if (c != null) fav.Items.Add(c);
                }
                if (fav.Items.Count > 0) _sections.Add(fav);
            }
            for (int s = 0; s < Cheats.SectionCount; s++)
            {
                SectionView v = new SectionView();
                v.Id = "c_" + Cheats.SectionIds[s];
                v.Title = Cheats.SectionTitle(s);
                for (int i = 0; i < Cheats.All.Count; i++)
                {
                    Cheat c = Cheats.All[i];
                    if (c.Section == s && Cheats.Matches(c, _vSearch)) v.Items.Add(c);
                }
                if (v.Items.Count > 0) _sections.Add(v);
            }
        }

        private static string TabName(int i)
        {
            switch (i)
            {
                case 0: return L.T("Trucos", "Cheats");
                case 1: return L.T("Valores", "Values");
                case 2: return L.T("Personajes", "Characters");
                default: return L.T("Ajustes", "Settings");
            }
        }

        private void DrawWindow(int id)
        {
            if (Event.current.type == EventType.Layout) TakeSnapshot();

            GUILayout.BeginHorizontal();
            for (int i = 0; i < TabCount; i++)
            {
                if (GUILayout.Toggle(_tab == i, TabName(i), "Button")) _tab = i;
            }
            GUILayout.EndHorizontal();

            // contexto: a cuantos soldados llega lo que hagas y si hay una mision en curso
            GUILayout.Label(L.T("Alcance: ", "Scope: ") + _scope + L.T(" soldados", " soldiers") + "   |   " +
                            (_inMission ? L.T("misión en curso", "mission in progress") : L.T("fuera de misión (campaña)", "no mission (campaign)")),
                            _ui.Hint);

            _scrolls[_drawTab] = GUILayout.BeginScrollView(_scrolls[_drawTab], GUILayout.ExpandHeight(true));
            if (_drawTab == 0) DrawCheats();
            else if (_drawTab == 1) _values.Draw();
            else if (_drawTab == 2) _characters.Draw();
            else DrawSettings();
            GUILayout.EndScrollView();

            // Descripcion / nombre interno del elemento bajo el mouse (Valores y Personajes): etiqueta SIEMPRE presente en
            // esas pestanas (con o sin texto) para que no cambie la cantidad de controles segun donde este el mouse.
            if (_drawTab == 1 || _drawTab == 2)
                GUILayout.Label(GUI.tooltip, _ui.Hint, GUILayout.Height(Mathf.Max(36f, _ui.Px * 3.6f)));

            GUILayout.Space(2);
            GUILayout.Label(_status);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(L.T("Ocultar (F10)", "Hide (F10)"))) _show = false;
            if (GUILayout.Button(L.T("Descargar trainer", "Unload trainer"))) Unload();
            GUILayout.EndHorizontal();
            GUILayout.Space(14);

            DoResizeHandle();
            GUI.DragWindow(new Rect(0, 0, 10000, 22));
        }

        // Esquina inferior derecha: arrastrarla cambia el tamano de la ventana.
        private void DoResizeHandle()
        {
            Rect handle = new Rect(_winW - 24f, _winH - 22f, 20f, 18f);
            int cid = GUIUtility.GetControlID(FocusType.Passive);
            Event e = Event.current;
            switch (e.GetTypeForControl(cid))
            {
                case EventType.MouseDown:
                    if (e.button == 0 && handle.Contains(e.mousePosition))
                    {
                        GUIUtility.hotControl = cid;
                        _grab = new Vector2(_winW - e.mousePosition.x, _winH - e.mousePosition.y);
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == cid)
                    {
                        _winW = Mathf.Clamp(e.mousePosition.x + _grab.x, 460f, Mathf.Max(460f, Screen.width));
                        _winH = Mathf.Clamp(e.mousePosition.y + _grab.y, 380f, Mathf.Max(380f, Screen.height));
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == cid)
                    {
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    break;
                case EventType.Repaint:
                    GUI.Box(handle, "::");
                    break;
            }
        }

        // ------------------------------------------------------------------ pestana Trucos

        private void DrawCheats()
        {
            GUILayout.Label(L.T(
                "Se aplican a tus soldados (tu tropa y los de tu bando en misión), nunca al enemigo. \"Misión\" = el juego lo reinicia en cada misión; \"campaña\" = se arrastra entre misiones. " +
                "Los marcados \"misión\" solo hacen efecto dentro de una misión.",
                "They apply to your soldiers (your troop and your side in a mission), never to the enemy. \"Mission\" = the game resets it every mission; \"campaign\" = it carries over between missions. " +
                "Items tagged \"mission\" only take effect inside a mission."), _ui.Hint);

            // perfiles rapidos
            GUILayout.BeginHorizontal();
            GUILayout.Label(L.T("Perfiles rápidos:", "Quick profiles:"), GUILayout.Width(Mathf.Max(100f, _ui.Px * 9f)));
            for (int i = 0; i < Cheats.Presets.Length; i++)
            {
                if (GUILayout.Button(Cheats.Presets[i].Name)) ApplyPreset(Cheats.Presets[i]);
            }
            GUILayout.EndHorizontal();

            // perfiles propios (3 ranuras)
            GUILayout.BeginHorizontal();
            GUILayout.Label(L.T("Mis perfiles:", "My profiles:"), GUILayout.Width(Mathf.Max(100f, _ui.Px * 9f)));
            for (int k = 0; k < Settings.ProfileSlots; k++)
            {
                GUI.enabled = _cfg.ProfileSet[k];
                string txt = L.T("Cargar ", "Load ") + (k + 1) + (_cfg.ProfileSet[k] ? " (" + _cfg.Profiles[k].Count + ")" : "");
                if (GUILayout.Button(txt)) LoadProfile(k);
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(L.T("Guardar los activos en:", "Save the active ones to:"), GUILayout.Width(Mathf.Max(100f, _ui.Px * 9f)));
            for (int k = 0; k < Settings.ProfileSlots; k++)
            {
                if (GUILayout.Button(L.T("Ranura ", "Slot ") + (k + 1))) SaveProfile(k);
            }
            GUILayout.EndHorizontal();

            // buscador + contador
            GUILayout.BeginHorizontal();
            GUILayout.Label(L.T("Buscar:", "Search:"), GUILayout.Width(Mathf.Max(55f, _ui.Px * 5f)));
            _search = GUILayout.TextField(_search);
            GUILayout.EndHorizontal();

            int active = Cheats.ActiveCount();
            Color old = GUI.contentColor;
            if (active > 0) GUI.contentColor = Ui.Green;
            GUILayout.BeginHorizontal();
            GUILayout.Label(active + L.T(" activos de ", " active of ") + Cheats.All.Count, _ui.Header);
            GUI.contentColor = old;
            if (GUILayout.Button(L.T("Plegar todo", "Collapse all"), GUILayout.Width(Mathf.Max(90f, _ui.Px * 8f)))) SetAllFolds(true);
            if (GUILayout.Button(L.T("Desplegar todo", "Expand all"), GUILayout.Width(Mathf.Max(90f, _ui.Px * 8f)))) SetAllFolds(false);
            GUILayout.EndHorizontal();

            if (_sections.Count == 0)
            {
                GUILayout.Label(L.T("Ningún truco coincide con la búsqueda.", "No cheat matches the search."));
                return;
            }

            bool searching = _vSearch.Length > 0;
            for (int s = 0; s < _sections.Count; s++)
            {
                SectionView sv = _sections[s];
                int on = 0;
                for (int i = 0; i < sv.Items.Count; i++) if (sv.Items[i].On) on++;
                string title = sv.Title + " (" + on + "/" + sv.Items.Count + ")";
                if (!_ui.FoldHeader(_cfg, sv.Id, title, !searching)) continue;
                for (int i = 0; i < sv.Items.Count; i++) DrawCheatRow(sv.Items[i], sv.IsFav ? i : -1);
            }

            if (_cfg.FavCheats.Count == 0 && !searching)
                GUILayout.Label(L.T("Tip: tocá \"Fav\" en un truco para fijarlo arriba y activarlo con Alt+1..9.",
                                    "Tip: press \"Fav\" on a cheat to pin it on top and toggle it with Alt+1..9."), _ui.Hint);
        }

        private void SetAllFolds(bool collapse)
        {
            List<string> ids = new List<string>();
            ids.Add("c_fav");
            for (int s = 0; s < Cheats.SectionCount; s++) ids.Add("c_" + Cheats.SectionIds[s]);
            for (int i = 0; i < ids.Count; i++)
            {
                if (collapse) _cfg.Collapsed.Add(ids[i]); else _cfg.Collapsed.Remove(ids[i]);
            }
            _cfg.Touch();
        }

        private void DrawCheatRow(Cheat c, int favIndex)
        {
            bool dim = c.MissionOnly && !_inMission && !c.On;

            GUILayout.BeginHorizontal();
            Color old = GUI.contentColor;
            if (c.On) GUI.contentColor = Ui.Green;
            else if (dim) GUI.contentColor = Ui.Gray;

            string label = c.Title;
            if (c.MissionOnly) label += "   · " + L.T("misión", "mission");
            if (favIndex >= 0 && favIndex < 9 && _cfg.Hotkeys) label = "[Alt+" + (favIndex + 1) + "] " + label;
            bool v = GUILayout.Toggle(c.On, label, _ui.Toggle);
            GUI.contentColor = old;
            if (v != c.On) c.On = v;

            bool isFav = _cfg.FavCheats.Contains(c.Id);
            bool fav = _ui.FavToggle(isFav);
            if (fav != isFav) _cfg.SetListed(_cfg.FavCheats, c.Id, fav);
            GUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(c.DescEs))
            {
                Color old2 = GUI.contentColor;
                if (dim) GUI.contentColor = Ui.Gray;
                GUILayout.Label(c.Desc, _ui.Desc);
                GUI.contentColor = old2;
            }
        }

        // ------------------------------------------------------------------ pestana Ajustes

        private bool Setting(bool value, string text)
        {
            bool v = GUILayout.Toggle(value, text, _ui.Toggle);
            if (v != value) _cfg.Touch();
            return v;
        }

        private void SetLanguage(int lang)
        {
            L.Lang = lang;
            _cfg.Language = lang;
            _cfg.Touch();
            _langDirty = true;
            _status = L.T("Idioma: ", "Language: ") + L.Names[lang];
        }

        private void DrawSettings()
        {
            GUILayout.Label(L.T("Idioma del trainer", "Trainer language"), _ui.Header);
            int picked = GUILayout.SelectionGrid(L.Lang, L.Names, 3);
            if (picked != L.Lang) SetLanguage(picked);
            GUILayout.Label(L.T("Los nombres de habilidades y estados de los personajes vienen del juego y siguen el idioma que elegiste en el juego.",
                                "Skill and status names come from the game and follow the language you picked in the game."), _ui.Hint);

            GUILayout.Label(L.T("Ventana", "Window"), _ui.Header);
            GUILayout.BeginHorizontal();
            GUILayout.Label(L.T("Transparencia: ", "Opacity: ") + Mathf.RoundToInt(_cfg.Alpha * 100f) + "%", GUILayout.Width(Mathf.Max(140f, _ui.Px * 11f)));
            float a = GUILayout.HorizontalSlider(_cfg.Alpha, 0.35f, 1f);
            GUILayout.EndHorizontal();
            if (Mathf.Abs(a - _cfg.Alpha) > 0.001f) { _cfg.Alpha = a; _cfg.Touch(); }

            GUILayout.BeginHorizontal();
            GUILayout.Label(L.T("Tamaño de letra: ", "Font size: ") + _ui.Px + " px", GUILayout.Width(Mathf.Max(140f, _ui.Px * 11f)));
            float f = GUILayout.HorizontalSlider(_cfg.FontScale, 0.8f, 1.8f);
            GUILayout.EndHorizontal();
            if (Mathf.Abs(f - _cfg.FontScale) > 0.001f) { _cfg.FontScale = f; _cfg.Touch(); }

            GUILayout.Label(L.T("Podés cambiar el tamaño de la ventana arrastrando la esquina inferior derecha (::) y moverla desde la barra de título. Se recuerdan.",
                                "You can resize the window by dragging the bottom-right corner (::) and move it by its title bar. Both are remembered."), _ui.Hint);
            if (GUILayout.Button(L.T("Restablecer posición y tamaño de la ventana", "Reset window position and size")))
            {
                _winW = 620f;
                _winH = Mathf.Min(700f, Mathf.Max(380f, Screen.height - 40f));
                _window = new Rect(20f, 20f, _winW, _winH);
                _cfg.Touch();
            }

            GUILayout.Label(L.T("Opciones", "Options"), _ui.Header);
            _cfg.RememberCheats = Setting(_cfg.RememberCheats,
                L.T("Recordar los trucos activos al volver a inyectar el trainer", "Remember the active cheats when the trainer is injected again"));
            _cfg.ShowHud = Setting(_cfg.ShowHud,
                L.T("Mostrar un indicador chico (\"AQT: N activos\") cuando el menú está oculto", "Show a small indicator (\"AQT: N active\") while the menu is hidden"));
            _cfg.Hotkeys = Setting(_cfg.Hotkeys,
                L.T("Teclas rápidas Alt+1..9 para los trucos favoritos", "Alt+1..9 hotkeys for favorite cheats"));
            _cfg.ShowIcons = Setting(_cfg.ShowIcons,
                L.T("Mostrar los íconos de los atributos en Personajes", "Show attribute icons in Characters"));

            GUILayout.Label(L.T("Teclas rápidas", "Hotkeys"), _ui.Header);
            GUILayout.Label("F10 / Insert: " + L.T("mostrar u ocultar el menú", "show or hide the menu"), _ui.Hint);
            // una sola etiqueta (con saltos de linea) para que la cantidad de controles no dependa de cuantos favoritos haya
            string keys;
            if (_cfg.FavCheats.Count == 0)
            {
                keys = L.T("Alt+1..9: marcá trucos con \"Fav\" en la pestaña Trucos para asignarles una tecla.",
                           "Alt+1..9: mark cheats with \"Fav\" in the Cheats tab to assign them a key.");
            }
            else
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                for (int k = 0; k < 9 && k < _cfg.FavCheats.Count; k++)
                {
                    Cheat c = Cheats.Get(_cfg.FavCheats[k]);
                    if (c == null) continue;
                    if (sb.Length > 0) sb.Append('\n');
                    sb.Append("Alt+").Append(k + 1).Append(": ").Append(c.Title);
                }
                keys = sb.ToString();
            }
            GUILayout.Label(keys, _ui.Hint);

            GUILayout.Label(L.T("Archivos", "Files"), _ui.Header);
            GUILayout.Label(L.T("Ajustes: ", "Settings: ") + Settings.FilePath, _ui.Hint);
            GUILayout.Label(L.T("Registro de errores: ", "Error log: ") + Log.Path, _ui.Hint);
        }

        // ------------------------------------------------------------------ descarga

        private void Unload()
        {
            try
            {
                RestoreAttributes();
                if (_cfg != null) _cfg.Save();
                AppDomain.CurrentDomain.SetData(Loader.DomainKey, null);
                Loader.ResetForReload();
                Log.Info("Trainer descargado desde el menu.");
            }
            catch (Exception e)
            {
                Log.Error("Unload", e);
            }
            Destroy(gameObject);
        }
    }
}
