using System;
using System.Collections.Generic;
using System.Text;

namespace AQT
{
    // Nombres legibles y grupos para los valores globales del juego (en el juego solo tienen un nombre interno,
    // p. ej. "HospitalTrust"). Los que no estan en la tabla se "humanizan" (Turn_Counter -> "Turn Counter").
    internal static class ValueNames
    {
        public const int GroupCount = 6;

        public static string GroupId(int g)
        {
            return "vg" + g;
        }

        public static string GroupTitle(int g)
        {
            switch (g)
            {
                case 0: return L.T("Recursos y suministros", "Resources and supplies");
                case 1: return L.T("Confianza", "Trust");
                case 2: return L.T("Probabilidades", "Chances");
                case 3: return L.T("Contadores y turnos", "Counters and turns");
                case 4: return L.T("Misión, trinchera y clima", "Mission, trench and weather");
                default: return L.T("Otros", "Other");
            }
        }

        private sealed class Pair
        {
            public string Es, En;
            public Pair(string es, string en) { Es = es; En = en; }
        }

        private static readonly Dictionary<string, Pair> Known = new Dictionary<string, Pair>();

        private static readonly HashSet<string> Resources = new HashSet<string>(new string[]
        {
            "Provision", "TrenchProvision", "QuartermasterSupply", "VillageFood", "Prestige", "HospitalCapacity",
            "AvailableAmmunitionValue", "AvailableFirstAidValue", "AvailableHandGrenadeValue", "AvailableSmokeGrenadeValue",
            "CollectedDogTags", "LootedProvisions", "LootedQuartermasterSupplies", "ProvisionCurrentConsumption", "Panic"
        });

        static ValueNames()
        {
            K("Provision", "Provisiones de la compañía", "Company provisions");
            K("TrenchProvision", "Provisiones de trinchera", "Trench provisions");
            K("QuartermasterSupply", "Suministros del intendente", "Quartermaster supplies");
            K("VillageFood", "Comida de la aldea", "Village food");
            K("Prestige", "Prestigio", "Prestige");
            K("HospitalCapacity", "Capacidad del hospital", "Hospital capacity");
            K("AvailableAmmunitionValue", "Munición disponible (reserva)", "Available ammunition (reserve)");
            K("AvailableFirstAidValue", "Vendas disponibles (reserva)", "Available bandages (reserve)");
            K("AvailableHandGrenadeValue", "Granadas de mano disponibles", "Available hand grenades");
            K("AvailableSmokeGrenadeValue", "Granadas de humo disponibles", "Available smoke grenades");
            K("CollectedDogTags", "Chapas de identificación recolectadas", "Collected dog tags");
            K("Panic", "Pánico", "Panic");
            K("PanicReduction", "Reducción de pánico", "Panic reduction");
            K("HospitalTrust", "Confianza: hospital", "Trust: hospital");
            K("QuartermasterTrust", "Confianza: intendente", "Trust: quartermaster");
            K("VillageTrust", "Confianza: aldea", "Trust: village");
            K("Disease_Dysentery_Chance", "Probabilidad de disentería", "Dysentery chance");
            K("Injury_TrenchFoot_Chance", "Probabilidad de pie de trinchera", "Trench foot chance");
            K("TroopReinforcementChance", "Probabilidad de refuerzos", "Reinforcement chance");
            K("MissionAttackChance", "Probabilidad de misión de ataque", "Attack mission chance");
            K("MissionDefenseChance", "Probabilidad de misión de defensa", "Defense mission chance");
            K("MutinyCounter", "Contador de motín", "Mutiny counter");
            K("TurnsUntilHomeLeave", "Turnos hasta el permiso", "Turns until home leave");
            K("TurnsUntilNextResupply", "Turnos hasta el reabastecimiento", "Turns until next resupply");
            K("TurnCounter_Rations_None", "Turnos seguidos sin raciones", "Turns in a row without rations");
            K("TurnCounter_Rations_Low", "Turnos seguidos con raciones bajas", "Turns in a row on low rations");
            K("TurnCounter_Rations_Standard", "Turnos seguidos con raciones normales", "Turns in a row on standard rations");
            K("TurnCounter_Rations_High", "Turnos seguidos con raciones altas", "Turns in a row on high rations");
            K("WindStrength", "Fuerza del viento", "Wind strength");
            K("WindDirection", "Dirección del viento", "Wind direction");
            K("Mission_VisionDistance", "Distancia de visión (misión)", "Vision distance (mission)");
        }

        private static void K(string name, string es, string en)
        {
            Known[name] = new Pair(es, en);
        }

        public static string Friendly(string name)
        {
            Pair p;
            if (Known.TryGetValue(name, out p)) return L.T(p.Es, p.En);
            return Humanize(name);
        }

        // Texto en ambos idiomas, para que el buscador encuentre "provisiones" y "provisions".
        public static string AllNames(string name)
        {
            Pair p;
            if (Known.TryGetValue(name, out p)) return p.Es + " " + p.En + " " + Friendly(name) + " " + name;
            return Humanize(name) + " " + name;
        }

        public static int GroupOf(string name)
        {
            if (Resources.Contains(name)) return 0;
            if (name.IndexOf("Trust", StringComparison.Ordinal) >= 0) return 1;
            if (name.IndexOf("Chance", StringComparison.Ordinal) >= 0) return 2;
            if (name.StartsWith("Mission", StringComparison.Ordinal) || name.StartsWith("Trench", StringComparison.Ordinal) ||
                name.IndexOf("Artillery", StringComparison.Ordinal) >= 0 || name.StartsWith("Wind", StringComparison.Ordinal) ||
                name.IndexOf("Weather", StringComparison.Ordinal) >= 0 || name.StartsWith("Camp", StringComparison.Ordinal)) return 4;
            if (name.IndexOf("Counter", StringComparison.Ordinal) >= 0 || name.IndexOf("Limit", StringComparison.Ordinal) >= 0 ||
                name.IndexOf("Turns", StringComparison.Ordinal) >= 0 || name.IndexOf("Remaining", StringComparison.Ordinal) >= 0) return 3;
            return 5;
        }

        // "TurnCounter_Rations_Low" -> "Turn Counter Rations Low"
        private static string Humanize(string name)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (c == '_') { sb.Append(' '); continue; }
                if (i > 0 && char.IsUpper(c) && char.IsLower(name[i - 1])) sb.Append(' ');
                sb.Append(c);
            }
            return sb.ToString().Trim();
        }
    }
}
