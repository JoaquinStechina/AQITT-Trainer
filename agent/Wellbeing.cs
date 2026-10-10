using System;
using System.Collections.Generic;
using AllQuietInTheTrenches;
using UnityEngine;

namespace AQT
{
    // "Wellbeing" cheats for the player's characters: hunger, mood (morale + optimism), health, fervor, fitness,
    // circumstances and relationships.
    //
    // Everything is done through the game's own API:
    //  - per-character values (SavedCharacterGameValue): Optimism, Fear, HealthImpairment, BleedingProcess,
    //    BleedingIntensityValue, FervorValue, Fitness, Morale
    //  - global values (SavedGameValue): Provision, TrenchProvision, TurnCounter_Rations_*
    //  - character attributes: CharacterRations_High (group CharacterRations), Health_Healthy (group HealthStatus),
    //    Fervor, and the wound/disease attributes that get removed.
    // Values and attributes are looked up by name only once (the managers' public arrays are indexed),
    // so we do not depend on any internal game variable, nor on what happens if a name does not exist.
    internal sealed class Wellbeing
    {
        private static readonly string[] SicknessNames = new string[]
        {
            "Disease_Dysentery", "Disease_Flu", "Disease_TrenchFever", "Disease_GasPoisoning", "Disease_Tuberculosis",
            "Complication_Fever", "Complication_Breathlessness", "Breathless"
        };

        private static readonly string[] HungerNames = new string[]
        {
            "CharacterTrenchProvision_None", "CharacterTrenchProvision_None_DefenseMission", "IndividualRations_Less"
        };

        // Circumstances (the game's "Circumstances" category): the bad ones are removed, the good ones applied.
        private static readonly string[] BadCircumstanceNames = new string[]
        {
            "Mood_Disappointed_Display", "Mood_Discordant_Display", "Mood_Disgruntled_Display", "Mood_Disgusted_Display",
            "Mood_Disheartened_Display", "Mood_Distressed_Display",
            "Grief_Display_1", "Grief_Display_2", "Grief_Display_3", "Grief_Display_4",
            "Circumstance_Bad_Display_1", "Circumstance_Bad_Display_2", "Circumstance_Bad_Display_3", "Circumstance_Bad_Display_4",
            "Mail_BadNews", "SoftBreakOrder_Frightened", "TargetForDeathNoRest", "LiceDiscovered"
        };

        private static readonly string[] GoodCircumstanceNames = new string[]
        {
            "CharacterWeather_Good_3", "Circumstance_Good_Display_4",
            "Mood_Determined_Display", "Mood_Grateful_Display", "Mood_Relieved_Display",
            "Motivated", "Mail_GoodNews"
        };

        private GameValueManager _gvm;
        private AttributeManager _am;
        private MissionImpactManager _mi;
        private MissionManager _mm;

        private readonly Dictionary<string, SavedCharacterGameValue> _charVals = new Dictionary<string, SavedCharacterGameValue>();
        private readonly Dictionary<string, SavedGameValue> _globalVals = new Dictionary<string, SavedGameValue>();
        private readonly Dictionary<string, CharacterAttribute> _attrs = new Dictionary<string, CharacterAttribute>();
        private readonly HashSet<CharacterAttribute> _badHealth = new HashSet<CharacterAttribute>();
        private readonly HashSet<CharacterAttribute> _badHunger = new HashSet<CharacterAttribute>();
        private readonly HashSet<CharacterAttribute> _badCirc = new HashSet<CharacterAttribute>();
        private readonly List<CharacterAttribute> _goodCirc = new List<CharacterAttribute>();
        private readonly HashSet<string> _warned = new HashSet<string>();
        private float _lastFriends = -99f;
        private bool _setsBuilt;

        // ------------------------------------------------------------------ setup

        // Returns false if the game data is not loaded yet.
        public bool Bind(GameValueManager gvm, AttributeManager am, MissionImpactManager mi, MissionManager mm)
        {
            if (gvm != _gvm)
            {
                _gvm = gvm;
                _charVals.Clear();
                _globalVals.Clear();
                IndexValues();
            }
            if (am != _am)
            {
                _am = am;
                _attrs.Clear();
                IndexAttributes();
                _setsBuilt = false;
            }
            if (mi != _mi || mm != _mm)
            {
                _mi = mi;
                _mm = mm;
                _setsBuilt = false;
            }
            if (_gvm == null || _am == null) return false;
            if (!_setsBuilt) BuildSets();
            return true;
        }

        private void IndexValues()
        {
            if (_gvm == null) return;

            CharacterGameValue[] cv = _gvm.CharacterGameValues;
            if (cv != null)
            {
                for (int i = 0; i < cv.Length; i++)
                {
                    SavedCharacterGameValue s = cv[i] as SavedCharacterGameValue;
                    if (s != null) _charVals[s.name] = s;
                }
            }

            MainGameValue[] mv = _gvm.MainGameValues;
            if (mv != null)
            {
                for (int i = 0; i < mv.Length; i++)
                {
                    SavedGameValue s = mv[i] as SavedGameValue;
                    if (s != null) _globalVals[s.name] = s;
                }
            }
        }

        private void IndexAttributes()
        {
            if (_am == null || _am.CharacterAttributes == null) return;
            for (int i = 0; i < _am.CharacterAttributes.Length; i++)
            {
                CharacterAttribute a = _am.CharacterAttributes[i];
                if (a != null) _attrs[a.name] = a;
            }
        }

        private void BuildSets()
        {
            _badHealth.Clear();
            _badHunger.Clear();

            if (_mi != null)
            {
                AddAll(_badHealth, _mi.AllInjuryAttributes);
                AddAll(_badHealth, _mi.BleedingInjuryAttributes);
                AddAll(_badHealth, _mi.DeadlyInjuryAttributes);
                AddAll(_badHealth, _mi.MildInjuryAttributes);
            }
            for (int i = 0; i < SicknessNames.Length; i++)
            {
                CharacterAttribute a = Attr(SicknessNames[i]);
                if (a != null) _badHealth.Add(a);
            }
            if (_mm != null && _mm.IsBleedingCharacterAttribute != null) _badHealth.Add(_mm.IsBleedingCharacterAttribute);

            // All of the game's complications (Skin rash, cough, nausea, pain, insomnia, gangrene, pneumonia, ...):
            // the game lists them next to wounds as "health problems" and they are not in the wound lists.
            // They are taken by prefix so we do not depend on a fixed list ("ComplicationHospitalization" does not match: no "_").
            foreach (KeyValuePair<string, CharacterAttribute> kv in _attrs)
            {
                if (kv.Value != null && kv.Key.StartsWith("Complication_", StringComparison.Ordinal))
                    _badHealth.Add(kv.Value);
            }

            for (int i = 0; i < HungerNames.Length; i++)
            {
                CharacterAttribute a = Attr(HungerNames[i]);
                if (a != null) _badHunger.Add(a);
            }

            _badCirc.Clear();
            _goodCirc.Clear();
            for (int i = 0; i < BadCircumstanceNames.Length; i++)
            {
                CharacterAttribute a = Attr(BadCircumstanceNames[i]);
                if (a != null) _badCirc.Add(a);
            }
            for (int i = 0; i < GoodCircumstanceNames.Length; i++)
            {
                CharacterAttribute a = Attr(GoodCircumstanceNames[i]);
                if (a != null) _goodCirc.Add(a);
            }

            _setsBuilt = true;
            Log.Info("Wellbeing: indexed " + _badHealth.Count + " health attributes and " + _badHunger.Count + " hunger attributes.");
        }

        private static void AddAll(HashSet<CharacterAttribute> set, List<CharacterAttribute> src)
        {
            if (src == null) return;
            for (int i = 0; i < src.Count; i++)
                if (src[i] != null) set.Add(src[i]);
        }

        private void Warn(string what)
        {
            if (_warned.Add(what)) Log.Info("Wellbeing: could not find " + what + " (skipping that part).");
        }

        private SavedCharacterGameValue CV(string name)
        {
            SavedCharacterGameValue v;
            if (_charVals.TryGetValue(name, out v)) return v;
            Warn("el valor de personaje '" + name + "'");
            return null;
        }

        private SavedGameValue GV(string name)
        {
            SavedGameValue v;
            if (_globalVals.TryGetValue(name, out v)) return v;
            Warn("el valor global '" + name + "'");
            return null;
        }

        private CharacterAttribute Attr(string name)
        {
            CharacterAttribute a;
            if (_attrs.TryGetValue(name, out a)) return a;
            Warn("el atributo '" + name + "'");
            return null;
        }

        // ------------------------------------------------------------------ global value helpers

        private static void GlobalAtLeast(SavedGameValue v, int fallback)
        {
            if (v == null) return;
            int target = v.UseMaxValue ? v.MaxValue : fallback;
            if (v.GetValue() < target) v.SetValue(target);
        }

        private static void GlobalAtMost(SavedGameValue v, int target)
        {
            if (v == null) return;
            if (v.GetValue() > target) v.SetValue(target);
        }

        // Moves a global value to its minimum (or to "fallback" if it has no minimum defined).
        private static void GlobalToMin(SavedGameValue v, int fallback)
        {
            if (v == null) return;
            GlobalAtMost(v, v.UseMinValue ? v.MinValue : fallback);
        }

        private int RemoveFrom(Character c, HashSet<CharacterAttribute> bad)
        {
            List<CharacterAttribute> types = _am.GetCharacterAttributeTypes(c);
            if (types == null || types.Count == 0) return 0;
            List<CharacterAttribute> copy = new List<CharacterAttribute>(types);
            int n = 0;
            for (int i = 0; i < copy.Count; i++)
            {
                if (copy[i] != null && bad.Contains(copy[i]))
                    n += Game.RemoveAttribute(_am, c, copy[i]);
            }
            return n;
        }

        // ------------------------------------------------------------------ the cheats

        // No hunger: rations at maximum for every character and company provisions full.
        public void NoHunger(List<Character> chars)
        {
            // Company and trench-camp provisions: full, and bad-rations counters at 0.
            GlobalAtLeast(GV("Provision"), 144);
            GlobalAtLeast(GV("TrenchProvision"), 100);
            GlobalAtMost(GV("TurnCounter_Rations_None"), 0);
            GlobalAtMost(GV("TurnCounter_Rations_Low"), 0);

            CharacterAttribute high = Attr("CharacterRations_High");
            for (int i = 0; i < chars.Count; i++)
            {
                Character c = chars[i];
                RemoveFrom(c, _badHunger);
                // CharacterRations is a group: adding "High" replaces None/Low/Standard.
                if (high != null) Game.AddAttribute(_am, c, high);
            }
        }

        // Happy: optimism and morale at maximum. (There used to be a separate "Max morale" checkbox that wrote
        // the same value, and this one also lowered fear; fear of the commander is now only in Relations.)
        public void Happy(List<Character> chars, MissionManager mm)
        {
            SavedCharacterGameValue opt = CV("Optimism");
            if (opt != null) Game.ToBest(opt, chars, true, 4);
            if (mm != null && mm.MoraleGameValue != null) Game.ToBest(mm.MoraleGameValue, chars, true, 20);
        }

        // Good health: no wounds, bleeding or diseases, and the "healthy" health state.
        public void Health(List<Character> chars, MissionManager mm)
        {
            // Chances of falling ill (global) and of hospital complications (per soldier): at the minimum.
            GlobalToMin(GV("Disease_Dysentery_Chance"), -10);
            GlobalToMin(GV("Injury_TrenchFoot_Chance"), -20);
            SavedCharacterGameValue complication = CV("HospitalComplicationChance");
            if (complication != null) Game.SetAtMost(complication, chars, -100);

            SavedCharacterGameValue imp = CV("HealthImpairment");
            if (imp != null) Game.ToBest(imp, chars, false, 0);
            if (mm != null)
            {
                Game.ToBest(mm.BleedingProcessGameValue, chars, false, 0);
                Game.ToBest(mm.BleedingIntensityGameValue, chars, false, 0);
            }

            CharacterAttribute healthy = mm != null ? mm.HealthyCharacterAttribute : null;
            CharacterAttribute dead = mm != null ? mm.DeathCharacterAttribute : null;

            for (int i = 0; i < chars.Count; i++)
            {
                Character c = chars[i];
                if (dead != null && _am.HasCharacterAttribute(c, dead)) continue;   // dead characters are left alone
                RemoveFrom(c, _badHealth);
                // HealthStatus is a group: adding "Healthy" replaces Impaired/RequiresTreatment/Critical.
                if (healthy != null) Game.AddAttribute(_am, c, healthy);
            }
        }

        // Good circumstances: removes the bad ones (grief, bad news, bad mood, lice...) and applies good weather,
        // good mood, "Motivated" and "positive events".
        public void Circumstances(List<Character> chars)
        {
            SavedCharacterGameValue lice = CV("Lice");
            if (lice != null) Game.ToBest(lice, chars, false, 0);

            for (int i = 0; i < chars.Count; i++)
            {
                Character c = chars[i];
                RemoveFrom(c, _badCirc);
                // CharacterWeather and Circumstance_Good_Display are groups: the new one replaces the previous one (e.g. bad weather).
                for (int k = 0; k < _goodCirc.Count; k++)
                    Game.AddAttribute(_am, c, _goodCirc[k]);
            }
        }

        // Soldiers' relationship with the player: trust at maximum and no fear (this yields "Relation: Comradely").
        public void Relations(List<Character> chars)
        {
            SavedCharacterGameValue trust = CV("Trust");
            if (trust != null) Game.ToBest(trust, chars, true, 99);
            SavedCharacterGameValue fear = CV("Fear");
            if (fear != null) Game.ToBest(fear, chars, false, 0);

            CharacterAttribute standing = Attr("PlayerStanding_Comradely");
            if (standing != null)
            {
                // PlayerStanding is a group: adding "Comradely" replaces the current level.
                for (int i = 0; i < chars.Count; i++)
                    Game.AddAttribute(_am, chars[i], standing);
            }
        }

        // Global trust with the hospital, the quartermaster and the village (the "relations" soldiers display).
        public void GroupTrust()
        {
            GlobalAtLeast(GV("HospitalTrust"), 99);
            GlobalAtLeast(GV("QuartermasterTrust"), 99);
            GlobalAtLeast(GV("VillageTrust"), 99);
        }

        // Rivalries between the player's soldiers: rivals become friends (the relationship is symmetric).
        public void Friends(List<Character> chars, CharacterManager cm)
        {
            if (cm == null) return;
            if (Time.unscaledTime - _lastFriends < 2f) return;   // no need to check this every 0.3 s
            _lastFriends = Time.unscaledTime;

            HashSet<Character> mine = new HashSet<Character>(chars);
            for (int i = 0; i < chars.Count; i++)
            {
                List<Character> rivals = cm.GetRelatedCharacters(chars[i], CharacterRelation.Rivals);
                if (rivals == null) continue;
                for (int k = 0; k < rivals.Count; k++)
                {
                    if (!mine.Contains(rivals[k])) continue;
                    cm.SetCharacterRelation(chars[i], rivals[k], CharacterRelation.Friends);
                    Log.Info("Rivals -> friends: " + chars[i].Forename + " " + chars[i].Surname + " / " + rivals[k].Forename + " " + rivals[k].Surname);
                }
            }
        }

        // Fervent: fervor at maximum (and the "Fervor" state applied).
        public void Fervor(List<Character> chars, MissionManager mm)
        {
            if (mm == null) return;
            if (mm.FervorGameValue != null) Game.ToBest(mm.FervorGameValue, chars, true, 100);
            if (mm.FervorAttribute != null)
            {
                for (int i = 0; i < chars.Count; i++)
                    Game.AddAttribute(_am, chars[i], mm.FervorAttribute);
            }
        }

        // Good physical condition: Fitness at maximum. More fitness = less exertion and less bleeding.
        public void Fitness(List<Character> chars, MissionManager mm)
        {
            if (mm == null || mm.FitnessGameValue == null) return;
            Game.ToBest(mm.FitnessGameValue, chars, true, 20);
        }
    }
}
