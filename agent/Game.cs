using System;
using System.Collections.Generic;
using AllQuietInTheTrenches;
using UnityEngine;

namespace AQT
{
    // Access to the game's own API (Assembly-CSharp). The game's "Manager" classes are
    // ScriptableObjects (AlienCore.Manager : GameScriptableObject), so they are found with
    // Resources.FindObjectsOfTypeAll, preferring the one with IsEnabled == true.
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

        // "Own" soldiers: the player's troop (in troop + reserves) and, during a mission,
        // all troops on YOUR side. NOTE: MissionManager.OwnTroops changes depending on whose
        // turn it is (on the enemy turn "Own" means the enemies), so it is NOT used; AllTroops
        // is filtered by the side (IsEntente) of the player's troop instead.
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

        // Refills a consumable: if the value has a cap it is raised to the cap; otherwise to a high value.
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

        // Adds an attribute if the character does not have it (group attributes replace the previous one in the group,
        // which the game itself resolves in AddCharacterAttributeData).
        public static bool AddAttribute(AttributeManager am, Character c, CharacterAttribute a)
        {
            if (am == null || c == null || a == null) return false;
            if (am.HasCharacterAttribute(c, a)) return false;
            am.AddCharacterAttributeData(c, a);
            return true;
        }

        // Removes all instances of the attribute (some are stackable). Returns how many were removed.
        public static int RemoveAttribute(AttributeManager am, Character c, CharacterAttribute a)
        {
            if (am == null || c == null || a == null) return 0;
            List<CharacterAttributeData> found = am.GetCharacterAttributeDatasets(c, a);
            if (found == null || found.Count == 0) return 0;
            List<CharacterAttributeData> copy = new List<CharacterAttributeData>(found);
            am.RemoveCharacterAttributeDatasets(copy);
            return copy.Count;
        }

        // Sets or removes a character attribute (e.g. InvincibleWound).
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
