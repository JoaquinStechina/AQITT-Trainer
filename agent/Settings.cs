using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace AQT
{
    // Trainer settings, stored in %APPDATA%\AQTrainer\settings.ini (plain text "key=value").
    // Loaded on injection and saved automatically shortly after every change.
    internal sealed class Settings
    {
        public const int ProfileSlots = 3;

        public int Language;                       // index into L.Codes: 0 es, 1 en, 2 fr, 3 pt, 4 ru, 5 de
        public bool RememberCheats = true;         // re-enable on injection what was active
        public bool ShowHud = true;                // small indicator while the menu is hidden
        public bool Hotkeys = true;                // Alt+1..9 for favorite cheats
        public bool ShowIcons = true;              // attribute icons in the Characters tab
        public float Alpha = 1f;                   // window opacity
        public float FontScale = 1f;               // font size
        public float WinX = -1f, WinY = -1f, WinW = 620f, WinH = 700f;
        public int Tab;

        public List<string> ActiveCheats = new List<string>();
        public List<string> FavCheats = new List<string>();
        public List<string> FavAttrs = new List<string>();
        public List<string> FavValues = new List<string>();
        public List<string>[] Profiles = new List<string>[ProfileSlots];
        public bool[] ProfileSet = new bool[ProfileSlots];
        public HashSet<string> Collapsed = new HashSet<string>();

        public bool Dirty;
        private float _changedAt;
        private static string _path;

        public Settings()
        {
            for (int i = 0; i < ProfileSlots; i++) Profiles[i] = new List<string>();
        }

        public static string FilePath
        {
            get
            {
                if (_path == null)
                {
                    try
                    {
                        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AQTrainer");
                        _path = Path.Combine(dir, "settings.ini");
                    }
                    catch
                    {
                        _path = Path.Combine(Path.GetTempPath(), "AQTrainer.settings.ini");
                    }
                }
                return _path;
            }
        }

        public void Touch()
        {
            Dirty = true;
            _changedAt = Time.unscaledTime;
        }

        // Saves if there were changes more than a second ago (so the file is not written every frame).
        public void SaveIfDue()
        {
            if (Dirty && Time.unscaledTime - _changedAt > 1f) Save();
        }

        // ------------------------------------------------------------------ load / save

        public static Settings Load()
        {
            Settings s = new Settings();
            try
            {
                if (!File.Exists(FilePath)) return s;
                string[] lines = File.ReadAllLines(FilePath, Encoding.UTF8);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = line.Substring(0, eq).Trim().ToLowerInvariant();
                    string val = line.Substring(eq + 1).Trim();
                    s.Apply(key, val);
                }
                s.Sanitize();
                Log.Info("Settings loaded from " + FilePath);
            }
            catch (Exception e)
            {
                Log.Error("Settings.Load", e);
            }
            return s;
        }

        private void Apply(string key, string val)
        {
            switch (key)
            {
                case "lang": Language = L.FromCode(val.ToLowerInvariant()); break;
                case "remember": RememberCheats = ParseBool(val, RememberCheats); break;
                case "hud": ShowHud = ParseBool(val, ShowHud); break;
                case "hotkeys": Hotkeys = ParseBool(val, Hotkeys); break;
                case "icons": ShowIcons = ParseBool(val, ShowIcons); break;
                case "alpha": Alpha = ParseFloat(val, Alpha); break;
                case "font": FontScale = ParseFloat(val, FontScale); break;
                case "winx": WinX = ParseFloat(val, WinX); break;
                case "winy": WinY = ParseFloat(val, WinY); break;
                case "winw": WinW = ParseFloat(val, WinW); break;
                case "winh": WinH = ParseFloat(val, WinH); break;
                case "tab": Tab = (int)ParseFloat(val, Tab); break;
                case "active": ActiveCheats = ParseList(val); break;
                case "favcheats": FavCheats = ParseList(val); break;
                case "favattrs": FavAttrs = ParseList(val); break;
                case "favvalues": FavValues = ParseList(val); break;
                case "collapsed": Collapsed = new HashSet<string>(ParseList(val)); break;
                default:
                    for (int p = 0; p < ProfileSlots; p++)
                    {
                        if (key == "profile" + (p + 1)) { Profiles[p] = ParseList(val); }
                        else if (key == "profile" + (p + 1) + "set") { ProfileSet[p] = ParseBool(val, false); }
                    }
                    break;
            }
        }

        private void Sanitize()
        {
            Alpha = Mathf.Clamp(Alpha, 0.35f, 1f);
            FontScale = Mathf.Clamp(FontScale, 0.8f, 1.8f);
            if (WinW < 460f) WinW = 460f;
            if (WinH < 380f) WinH = 380f;
            if (Tab < 0 || Tab > 3) Tab = 0;
        }

        public void Save()
        {
            Dirty = false;
            try
            {
                string dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# All Quiet in the Trenches - Trainer (generated automatically; safe to edit while the game is closed)");
                sb.AppendLine("lang=" + L.Codes[Math.Max(0, Math.Min(Language, L.Count - 1))]);
                sb.AppendLine("remember=" + (RememberCheats ? "1" : "0"));
                sb.AppendLine("hud=" + (ShowHud ? "1" : "0"));
                sb.AppendLine("hotkeys=" + (Hotkeys ? "1" : "0"));
                sb.AppendLine("icons=" + (ShowIcons ? "1" : "0"));
                sb.AppendLine("alpha=" + F(Alpha));
                sb.AppendLine("font=" + F(FontScale));
                sb.AppendLine("winx=" + F(WinX));
                sb.AppendLine("winy=" + F(WinY));
                sb.AppendLine("winw=" + F(WinW));
                sb.AppendLine("winh=" + F(WinH));
                sb.AppendLine("tab=" + Tab.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("active=" + string.Join(",", ActiveCheats.ToArray()));
                sb.AppendLine("favcheats=" + string.Join(",", FavCheats.ToArray()));
                sb.AppendLine("favattrs=" + string.Join(",", FavAttrs.ToArray()));
                sb.AppendLine("favvalues=" + string.Join(",", FavValues.ToArray()));
                string[] col = new string[Collapsed.Count];
                Collapsed.CopyTo(col);
                sb.AppendLine("collapsed=" + string.Join(",", col));
                for (int p = 0; p < ProfileSlots; p++)
                {
                    sb.AppendLine("profile" + (p + 1) + "=" + string.Join(",", Profiles[p].ToArray()));
                    sb.AppendLine("profile" + (p + 1) + "set=" + (ProfileSet[p] ? "1" : "0"));
                }
                File.WriteAllText(FilePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception e)
            {
                Log.Error("Settings.Save", e);
            }
        }

        // ------------------------------------------------------------------ utilities

        private static string F(float v)
        {
            return v.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static bool ParseBool(string s, bool fallback)
        {
            if (s == "1" || s == "true") return true;
            if (s == "0" || s == "false") return false;
            return fallback;
        }

        private static float ParseFloat(string s, float fallback)
        {
            float v;
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : fallback;
        }

        private static List<string> ParseList(string s)
        {
            List<string> r = new List<string>();
            if (string.IsNullOrEmpty(s)) return r;
            string[] parts = s.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i].Trim();
                if (p.Length > 0 && !r.Contains(p)) r.Add(p);
            }
            return r;
        }

        // Adds or removes a name from a favorites list. Returns the new state.
        public bool SetListed(List<string> list, string name, bool on)
        {
            bool has = list.Contains(name);
            if (on && !has) { list.Add(name); Touch(); }
            else if (!on && has) { list.Remove(name); Touch(); }
            return on;
        }
    }
}
