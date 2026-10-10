using System;
using System.Collections.Generic;
using AllQuietInTheTrenches;
using UnityEngine;

namespace AQT
{
    // Trucos de "bienestar" de los personajes del jugador: hambre, animo (moral + optimismo), salud, fervor, forma fisica,
    // circunstancias y relaciones.
    //
    // Todo se hace con la API del propio juego:
    //  - valores por personaje (SavedCharacterGameValue): Optimism, Fear, HealthImpairment, BleedingProcess,
    //    BleedingIntensityValue, FervorValue, Fitness, Morale
    //  - valores globales (SavedGameValue): Provision, TrenchProvision, TurnCounter_Rations_*
    //  - atributos de personaje: CharacterRations_High (grupo CharacterRations), Health_Healthy (grupo HealthStatus),
    //    Fervor, y los atributos de heridas/enfermedades que se quitan.
    // Los valores y atributos se buscan por nombre una sola vez (se indexan los arreglos publicos de los managers),
    // asi no dependemos de ninguna variable interna del juego ni de lo que pase si un nombre no existe.
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

        // Circunstancias (categoria "Circumstances" del juego): las malas se quitan, las buenas se ponen.
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

        // ------------------------------------------------------------------ preparacion

        // Devuelve false si todavia no hay datos del juego cargados.
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

            // Todas las complicaciones del juego (Skin rash, tos, nauseas, dolor, insomnio, gangrena, neumonia, ...):
            // el juego las lista junto a las heridas como "problemas de salud" y no estan en las listas de heridas.
            // Se toman por prefijo para no depender de una lista fija ("ComplicationHospitalization" no entra: no lleva "_").
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
            Log.Info("Bienestar: " + _badHealth.Count + " atributos de salud y " + _badHunger.Count + " de hambre indexados.");
        }

        private static void AddAll(HashSet<CharacterAttribute> set, List<CharacterAttribute> src)
        {
            if (src == null) return;
            for (int i = 0; i < src.Count; i++)
                if (src[i] != null) set.Add(src[i]);
        }

        private void Warn(string what)
        {
            if (_warned.Add(what)) Log.Info("Bienestar: no se encontro " + what + " (se omite esa parte).");
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

        // ------------------------------------------------------------------ helpers de valores globales

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

        // Lleva un valor global a su minimo (o a "fallback" si no tiene minimo definido).
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

        // ------------------------------------------------------------------ los trucos

        // Sin hambre: raciones al maximo para cada personaje y provisiones de la compania llenas.
        public void NoHunger(List<Character> chars)
        {
            // Provisiones de la compania y del campamento de trinchera: llenas, y contadores de raciones malas en 0.
            GlobalAtLeast(GV("Provision"), 144);
            GlobalAtLeast(GV("TrenchProvision"), 100);
            GlobalAtMost(GV("TurnCounter_Rations_None"), 0);
            GlobalAtMost(GV("TurnCounter_Rations_Low"), 0);

            CharacterAttribute high = Attr("CharacterRations_High");
            for (int i = 0; i < chars.Count; i++)
            {
                Character c = chars[i];
                RemoveFrom(c, _badHunger);
                // CharacterRations es un grupo: agregar "High" reemplaza None/Low/Standard.
                if (high != null) Game.AddAttribute(_am, c, high);
            }
        }

        // Felices: optimismo y moral al maximo. (Antes habia ademas una casilla "Moral al maximo" aparte, que escribia
        // el mismo valor, y esta tambien bajaba el miedo; el miedo al comandante ahora esta solo en Relations.)
        public void Happy(List<Character> chars, MissionManager mm)
        {
            SavedCharacterGameValue opt = CV("Optimism");
            if (opt != null) Game.ToBest(opt, chars, true, 4);
            if (mm != null && mm.MoraleGameValue != null) Game.ToBest(mm.MoraleGameValue, chars, true, 20);
        }

        // Buena salud: sin heridas, sangrado ni enfermedades, y estado de salud "sano".
        public void Health(List<Character> chars, MissionManager mm)
        {
            // Probabilidades de enfermar (globales) y de complicaciones en el hospital (por soldado): al minimo.
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
                if (dead != null && _am.HasCharacterAttribute(c, dead)) continue;   // a los muertos no se los toca
                RemoveFrom(c, _badHealth);
                // HealthStatus es un grupo: agregar "Healthy" reemplaza Impaired/RequiresTreatment/Critical.
                if (healthy != null) Game.AddAttribute(_am, c, healthy);
            }
        }

        // Circunstancias buenas: quita las malas (duelo, malas noticias, mal humor, piojos...) y pone buen clima,
        // buen humor, "Motivated" y "eventos positivos".
        public void Circumstances(List<Character> chars)
        {
            SavedCharacterGameValue lice = CV("Lice");
            if (lice != null) Game.ToBest(lice, chars, false, 0);

            for (int i = 0; i < chars.Count; i++)
            {
                Character c = chars[i];
                RemoveFrom(c, _badCirc);
                // CharacterWeather y Circumstance_Good_Display son grupos: el nuevo reemplaza al anterior (p. ej. mal clima).
                for (int k = 0; k < _goodCirc.Count; k++)
                    Game.AddAttribute(_am, c, _goodCirc[k]);
            }
        }

        // Relacion de los soldados con el jugador: confianza al maximo y sin miedo (de ahi sale "Relation: Comradely").
        public void Relations(List<Character> chars)
        {
            SavedCharacterGameValue trust = CV("Trust");
            if (trust != null) Game.ToBest(trust, chars, true, 99);
            SavedCharacterGameValue fear = CV("Fear");
            if (fear != null) Game.ToBest(fear, chars, false, 0);

            CharacterAttribute standing = Attr("PlayerStanding_Comradely");
            if (standing != null)
            {
                // PlayerStanding es un grupo: agregar "Comradely" reemplaza al nivel actual.
                for (int i = 0; i < chars.Count; i++)
                    Game.AddAttribute(_am, chars[i], standing);
            }
        }

        // Confianza global con el hospital, el intendente y la aldea (las "relaciones" que muestran los soldados).
        public void GroupTrust()
        {
            GlobalAtLeast(GV("HospitalTrust"), 99);
            GlobalAtLeast(GV("QuartermasterTrust"), 99);
            GlobalAtLeast(GV("VillageTrust"), 99);
        }

        // Rivalidades entre soldados del jugador: los rivales pasan a ser amigos (la relacion es simetrica).
        public void Friends(List<Character> chars, CharacterManager cm)
        {
            if (cm == null) return;
            if (Time.unscaledTime - _lastFriends < 2f) return;   // no hace falta revisarlo cada 0,3 s
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
                    Log.Info("Rivales -> amigos: " + chars[i].Forename + " " + chars[i].Surname + " / " + rivals[k].Forename + " " + rivals[k].Surname);
                }
            }
        }

        // Exaltados: fervor al maximo (y el estado "Fervor" puesto).
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

        // Buena condicion fisica: forma fisica (Fitness) al maximo. Mas forma fisica = menos esfuerzo y menos sangrado.
        public void Fitness(List<Character> chars, MissionManager mm)
        {
            if (mm == null || mm.FitnessGameValue == null) return;
            Game.ToBest(mm.FitnessGameValue, chars, true, 20);
        }
    }
}
