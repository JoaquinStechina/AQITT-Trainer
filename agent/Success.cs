using System;
using System.Collections.Generic;
using AllQuietInTheTrenches;
using UnityEngine;

namespace AQT
{
    // "Actions always succeed" for the player's soldiers.
    //
    // How the game decides (read from the IL of Assembly-CSharp): mission actions do not use a global roll but
    // per-character "efficiency" values (percentages, 0 = normal). Each roll looks like
    //     chance = base * max(0, 1 + efficiency/100)   and   success if  chance > Random.Range(0, 100)
    // Real examples:
    //   - Cutting barbed wire (MissionManager.DoRemoveObstacle): 70 * mod(DexterityBasedEfficiency) > Random(0,100)
    //   - Clearing a jammed rifle (formula FixRifleJammingSuccessProbability): 60 * (1 + DexterityBasedEfficiency*0.01)
    //   - Grenade (MissionGrenadeManager.HandleGrenadeImpact): Random(50,150)*mod(ThrowGrenadeEfficiency+distance) > 130 = direct hit
    //   - Aimed fire (MissionOverwatchManager.HandleAimedFire): 100*mod(BaseFireEfficiency)*mod(cover) > Random(0,100)
    //   - Melee (MissionMeleeManager.EvaluateMeleeMatchWinner): Random(0.5V,V)*mod(MeleeEfficiency) of the attacker vs. the defender's
    //   - First aid (DoFirstAid): amount treated * (1 + FirstAidEfficiency*0.01)
    //   - Looting (GetLootAmount): amount * mod(PerceptionBasedEfficiency)
    // By raising those values ONLY on your soldiers, the rolls always pass and the enemy stays the same.
    //
    // To avoid leaving the game "broken" a fixed amount (delta) is added and subtracted again when turned off; the game keeps
    // adding/subtracting its own modifiers on top without any problem.
    internal sealed class SuccessBoost
    {
        private sealed class Entry
        {
            public string Label;
            public SavedCharacterGameValue Value;
            public int Amount;
        }

        // What we already added, per value (if the MissionManager is recreated, the same value is not counted twice).
        private sealed class Applied
        {
            public int Amount;
            public HashSet<Character> Chars = new HashSet<Character>();
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private readonly Dictionary<SavedCharacterGameValue, Applied> _boosted = new Dictionary<SavedCharacterGameValue, Applied>();
        private MissionManager _mm;

        // true if there are soldiers with the boost applied (there is something to revert when turned off).
        public bool Active
        {
            get
            {
                foreach (KeyValuePair<SavedCharacterGameValue, Applied> kv in _boosted)
                    if (kv.Value.Chars.Count > 0) return true;
                return false;
            }
        }

        private void Build(MissionManager mm)
        {
            _entries.Clear();
            _mm = mm;
            Add("Disparo (base)", mm.BaseFireEfficiencyGameValue, 200);
            Add("Disparo (cobertura)", mm.CoverFireEfficiencyGameValue, 200);
            Add("Disparo (flanqueo)", mm.FlankFireEfficiencyGameValue, 200);
            Add("Disparo (vigilancia)", mm.OverwatchFireEfficiencyGameValue, 200);
            Add("Fuego de supresion", mm.SuppressFireEfficiencyGameValue, 200);
            Add("Primeros auxilios", mm.FirstAidEfficiencyGameValue, 300);
            Add("Alambre de puas / fusil trabado", mm.CutBarbwireEfficiencyGameValue, 300);
            Add("Saqueo", mm.LootEfficiencyValue, 200);
            Add("Granadas", mm.ThrowGrenadeEfficiencyValue, 400);
            Add("Cuerpo a cuerpo", mm.MeleeEfficiencyValue, 400);
        }

        private void Add(string label, CharacterGameValue v, int amount)
        {
            SavedCharacterGameValue s = v as SavedCharacterGameValue;
            if (s == null)
            {
                Log.Info("Actions: '" + label + "' is not an editable value (skipped).");
                return;
            }
            Entry e = new Entry();
            e.Label = label;
            e.Value = s;
            e.Amount = amount;
            _entries.Add(e);
        }

        public void Apply(MissionManager mm, List<Character> chars)
        {
            if (mm == null) return;
            if (_entries.Count == 0 || mm != _mm) Build(mm);

            for (int i = 0; i < _entries.Count; i++)
            {
                Entry e = _entries[i];
                Applied applied;
                if (!_boosted.TryGetValue(e.Value, out applied))
                {
                    applied = new Applied();
                    applied.Amount = e.Amount;
                    _boosted[e.Value] = applied;
                }
                HashSet<Character> set = applied.Chars;

                int threshold = e.Amount - 90;   // the value's minimum is -90: a boosted one is always >= Amount-90 (and normal ones stay below)
                for (int k = 0; k < chars.Count; k++)
                {
                    Character c = chars[k];
                    if (e.Value.GetValue(c) >= threshold)
                    {
                        set.Add(c);   // already boosted (by us earlier, or it comes from a save made that way)
                        continue;
                    }
                    e.Value.AddValue(c, e.Amount);
                    set.Add(c);
                }
            }
        }

        // Subtracts what we added. If the game already reset that value (it is below the threshold), it is left alone.
        public void Revert()
        {
            int restored = 0;
            foreach (KeyValuePair<SavedCharacterGameValue, Applied> kv in _boosted)
            {
                SavedCharacterGameValue value = kv.Key;
                int amount = kv.Value.Amount;
                int threshold = amount - 90;
                foreach (Character c in kv.Value.Chars)
                {
                    if (c == null) continue;
                    if (value.GetValue(c) >= threshold)
                    {
                        value.ReduceValue(c, amount);
                        restored++;
                    }
                }
                kv.Value.Chars.Clear();
            }
            _boosted.Clear();
            if (restored > 0) Log.Info("Actions: reverted " + restored + " efficiency values.");
        }
    }
}
