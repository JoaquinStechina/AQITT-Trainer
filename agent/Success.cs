using System;
using System.Collections.Generic;
using AllQuietInTheTrenches;
using UnityEngine;

namespace AQT
{
    // "Acciones siempre exitosas" para los soldados del jugador.
    //
    // Como decide el juego (leido del IL de Assembly-CSharp): las acciones de mision no usan una tirada global sino
    // valores de "eficiencia" por personaje (porcentajes, 0 = normal). Cada tirada es del estilo
    //     chance = base * max(0, 1 + eficiencia/100)   y   exito si  chance > Random.Range(0, 100)
    // Ejemplos reales:
    //   - Cortar alambre de puas (MissionManager.DoRemoveObstacle): 70 * mod(DexterityBasedEfficiency) > Random(0,100)
    //   - Arreglar fusil trabado (formula FixRifleJammingSuccessProbability): 60 * (1 + DexterityBasedEfficiency*0.01)
    //   - Granada (MissionGrenadeManager.HandleGrenadeImpact): Random(50,150)*mod(ThrowGrenadeEfficiency+distancia) > 130 = impacto directo
    //   - Disparo apuntado (MissionOverwatchManager.HandleAimedFire): 100*mod(BaseFireEfficiency)*mod(cobertura) > Random(0,100)
    //   - Melee (MissionMeleeManager.EvaluateMeleeMatchWinner): Random(0.5V,V)*mod(MeleeEfficiency) del atacante vs. el del defensor
    //   - Botiquin (DoFirstAid): cantidad tratada * (1 + FirstAidEfficiency*0.01)
    //   - Saqueo (GetLootAmount): cantidad * mod(PerceptionBasedEfficiency)
    // Subiendo esos valores SOLO en tus soldados, las tiradas pasan siempre y el enemigo queda igual.
    //
    // Para no dejar la partida "rota" se suma una cantidad fija (delta) y se la resta al desactivar; el juego sigue
    // sumando/restando sus propios modificadores encima sin problema.
    internal sealed class SuccessBoost
    {
        private sealed class Entry
        {
            public string Label;
            public SavedCharacterGameValue Value;
            public int Amount;
        }

        // Lo que ya sumamos, por valor (si el MissionManager se recrea, el mismo valor no se cuenta dos veces).
        private sealed class Applied
        {
            public int Amount;
            public HashSet<Character> Chars = new HashSet<Character>();
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private readonly Dictionary<SavedCharacterGameValue, Applied> _boosted = new Dictionary<SavedCharacterGameValue, Applied>();
        private MissionManager _mm;

        // true si hay soldados con la mejora puesta (hay algo que revertir al desactivar).
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
                Log.Info("Acciones: '" + label + "' no es un valor editable (se omite).");
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

                int threshold = e.Amount - 90;   // el minimo del valor es -90: potenciado siempre queda >= Amount-90 (y los normales quedan por debajo)
                for (int k = 0; k < chars.Count; k++)
                {
                    Character c = chars[k];
                    if (e.Value.GetValue(c) >= threshold)
                    {
                        set.Add(c);   // ya estaba potenciado (por nosotros antes, o viene de una partida guardada asi)
                        continue;
                    }
                    e.Value.AddValue(c, e.Amount);
                    set.Add(c);
                }
            }
        }

        // Resta lo que sumamos. Si el juego ya reseteo ese valor (queda bajo el umbral), no se toca.
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
            if (restored > 0) Log.Info("Acciones: revertidos " + restored + " valores de eficiencia.");
        }
    }
}
