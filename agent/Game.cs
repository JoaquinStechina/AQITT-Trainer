using System;
using System.Collections.Generic;
using AllQuietInTheTrenches;
using UnityEngine;

namespace AQT
{
    // Acceso a la API del propio juego (Assembly-CSharp). Los "Manager" del juego son
    // ScriptableObjects (AlienCore.Manager : GameScriptableObject), asi que se buscan con
    // Resources.FindObjectsOfTypeAll y se prefiere el que tenga IsEnabled == true.
    internal static class Game
    {
        public static T Find<T>() where T : ScriptableObject
        {
            T fallback = null;
            T[] all = Resources.FindObjectsOfTypeAll<T>();
            for (int i = 0; i < all.Length; i++)
            {
                T o = all[i];
                if (o == null) continue;
                AlienCore.Manager m = o as AlienCore.Manager;
                if (m != null && m.IsEnabled) return o;
                if (fallback == null) fallback = o;
            }
            return fallback;
        }

        // Soldados "propios": la tropa del jugador (en tropa + reserva) y, durante una mision,
        // todas las tropas de TU bando. OJO: MissionManager.OwnTroops cambia segun de quien sea
        // el turno (en el turno enemigo "Own" son los enemigos), por eso NO se usa; se filtra
        // AllTroops por el bando (IsEntente) de la tropa del jugador.
        public static List<Character> MyCharacters(CharacterManager cm, MissionManager mm)
        {
            HashSet<Character> set = new HashSet<Character>();
            List<Character> result = new List<Character>();

            if (cm == null || cm.PlayerTroop == null) return result;

            AddAll(set, result, cm.PlayerTroop.GetCharactersInTroop(CharacterStatus.InTroop, CharacterStatus.InReserves));

            if (mm != null && mm.IsEnabled)
            {
                List<Troop> all = mm.AllTroops;
                bool playerIsEntente = cm.PlayerTroop.IsEntente;
                if (all != null)
                {
                    for (int i = 0; i < all.Count; i++)
                    {
                        Troop t = all[i];
                        if (t == null || t.IsEntente != playerIsEntente) continue;
                        AddAll(set, result, mm.GetAllActiveCharacters(t));
                    }
                }
            }
            return result;
        }

        private static void AddAll(HashSet<Character> set, List<Character> result, List<Character> src)
        {
            if (src == null) return;
            for (int i = 0; i < src.Count; i++)
            {
                Character c = src[i];
                if (c != null && set.Add(c)) result.Add(c);
            }
        }

        // Recarga un consumible: si el valor tiene tope lo sube al tope; si no, a un valor alto.
        public static int TopUp(SavedCharacterGameValue v, List<Character> chars)
        {
            if (v == null) return 0;
            int target = v.UseMaxValue ? v.MaxValue : Math.Max(v.StartValue, 99);
            return SetAtLeast(v, chars, target);
        }

        public static int SetAtLeast(SavedCharacterGameValue v, List<Character> chars, int target)
        {
            int changed = 0;
            for (int i = 0; i < chars.Count; i++)
            {
                if (v.GetValue(chars[i]) < target)
                {
                    v.SetValue(chars[i], target);
                    changed++;
                }
            }
            return changed;
        }

        public static int SetAtMost(SavedCharacterGameValue v, List<Character> chars, int target)
        {
            int changed = 0;
            for (int i = 0; i < chars.Count; i++)
            {
                if (v.GetValue(chars[i]) > target)
                {
                    v.SetValue(chars[i], target);
                    changed++;
                }
            }
            return changed;
        }

        public static void ToBest(SavedCharacterGameValue v, List<Character> chars, bool highIsGood, int fallbackHigh)
        {
            if (v == null) return;
            if (highIsGood)
                SetAtLeast(v, chars, v.UseMaxValue ? v.MaxValue : fallbackHigh);
            else
                SetAtMost(v, chars, v.UseMinValue ? v.MinValue : 0);
        }

        // Agrega un atributo si el personaje no lo tiene (los de grupo reemplazan al anterior del grupo,
        // lo resuelve el propio juego en AddCharacterAttributeData).
        public static bool AddAttribute(AttributeManager am, Character c, CharacterAttribute a)
        {
            if (am == null || c == null || a == null) return false;
            if (am.HasCharacterAttribute(c, a)) return false;
            am.AddCharacterAttributeData(c, a);
            return true;
        }

        // Quita todas las instancias del atributo (algunos son apilables). Devuelve cuantas quito.
        public static int RemoveAttribute(AttributeManager am, Character c, CharacterAttribute a)
        {
            if (am == null || c == null || a == null) return 0;
            List<CharacterAttributeData> found = am.GetCharacterAttributeDatasets(c, a);
            if (found == null || found.Count == 0) return 0;
            List<CharacterAttributeData> copy = new List<CharacterAttributeData>(found);
            am.RemoveCharacterAttributeDatasets(copy);
            return copy.Count;
        }

        // Pone o quita un atributo de personaje (p. ej. InvincibleWound).
        public static void EnsureAttribute(AttributeManager am, List<Character> chars, CharacterAttribute attr, bool want)
        {
            if (am == null || attr == null) return;
            for (int i = 0; i < chars.Count; i++)
            {
                Character c = chars[i];
                bool has = am.HasCharacterAttribute(c, attr);
                if (want && !has)
                {
                    am.AddCharacterAttributeData(c, attr);
                }
                else if (!want && has)
                {
                    CharacterAttributeData d = am.GetCharacterAttributeData(c, attr);
                    if (d != null) am.RemoveCharacterAttributeData(d);
                }
            }
        }
    }
}
