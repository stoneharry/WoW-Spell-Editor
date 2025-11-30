using NLog;
using SpellEditor.Sources.Database;
using SpellEditor.Sources.DBC;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SpellEditor.Sources.AI
{
    /// <summary>
    /// Finds existing spells that are semantically similar to a given spell ID.
    /// Used to provide concrete examples to the AI for better spell generation.
    /// </summary>
    public static class AiSimilarSpellFinder
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Returns up to <paramref name="maxExamples"/> similar spells to <paramref name="spellId"/>,
        /// based on school, mechanic, effects, auras, targets, resource type, and family.
        /// If anything fails, returns an empty list.
        /// </summary>
        public static List<AiSimilarSpellSummary> FindSimilarSpells(string prompt, int maxExamples = 3)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return new List<AiSimilarSpellSummary>();

            // Extract semantic keywords from the prompt.
            var tokens = ExtractPromptKeywords(prompt);
            if (tokens.Count == 0)
                return new List<AiSimilarSpellSummary>();

            var result = new List<AiSimilarSpellSummary>();

            try
            {
                using (var adapter = AdapterFactory.Instance.GetAdapter(false))
                {
                    var all = adapter.Query(@"
                        SELECT
                            `ID`,
                            `SpellName0`,
                            `SpellDescription0`,
                            `SchoolMask`,
                            `Mechanic`,
                            `Effect1`,
                            `Effect2`,
                            `Effect3`,
                            `EffectApplyAuraName1`,
                            `EffectApplyAuraName2`,
                            `EffectApplyAuraName3`
                        FROM `spell`
                    ");

                    var scored = new List<Tuple<double, DataRow>>();

                    foreach (DataRow row in all.Rows)
                    {
                        double score = ComputePromptSimilarity(tokens, row);
                        if (score > 0)
                            scored.Add(Tuple.Create(score, row));
                    }

                    foreach (var tuple in scored
                        .OrderByDescending(t => t.Item1)
                        .Take(maxExamples))
                    {
                        var summary = BuildSemanticSummary(tuple.Item2);
                        if (summary != null)
                            result.Add(summary);
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "AiSimilarSpellFinder.FindSimilarSpells failed");
            }

            return result;
        }

        public static List<AiSimilarSpellSummary> FindSimilarSpellsFromPrompt(
            string prompt,
            int maxExamples = 3)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return new List<AiSimilarSpellSummary>();

            // Extract semantic keywords from the prompt.
            var tokens = ExtractPromptKeywords(prompt);
            if (tokens.Count == 0)
                return new List<AiSimilarSpellSummary>();

            var result = new List<AiSimilarSpellSummary>();

            try
            {
                using (var adapter = AdapterFactory.Instance.GetAdapter(false))
                {
                    var all = adapter.Query("SELECT * FROM `spell`");

                    var scored = new List<Tuple<double, DataRow>>();

                    foreach (DataRow row in all.Rows)
                    {
                        double score = ComputePromptSimilarity(tokens, row);
                        if (score > 0)
                            scored.Add(Tuple.Create(score, row));
                    }

                    foreach (var tuple in scored
                        .OrderByDescending(t => t.Item1)
                        .Take(maxExamples))
                    {
                        var summary = BuildSemanticSummary(tuple.Item2);
                        if (summary != null)
                            result.Add(summary);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "FindSimilarSpellsFromPrompt failed");
            }

            return result;
        }

        private static HashSet<string> ExtractPromptKeywords(string prompt)
        {
            var t = prompt.ToLowerInvariant();

            var set = new HashSet<string>();

            string[] possible = {
        "fire","frost","arcane","shadow","nature","holy","physical",
        "dot","hot","heal","damage","periodic","aoe","area","radius",
        "cone","chain","projectile","melee","instant","cast","channel",
        "stun","root","slow","silence","fear","charm","interrupt",
        "poison","disease","curse","magic",
        "mage","priest","shaman","warrior","druid","paladin","rogue","hunter","warlock","deathknight"
    };

            foreach (var word in possible)
            {
                if (t.Contains(word))
                    set.Add(word);
            }

            return set;
        }

        private static double ComputePromptSimilarity(HashSet<string> tokens, DataRow row)
        {
            double score = 0;

            string name = (row["SpellName0"] ?? "").ToString().ToLowerInvariant();
            string desc = (row["SpellDescription0"] ?? "").ToString().ToLowerInvariant();

            foreach (var t in tokens)
            {
                if (name.Contains(t))
                    score += 10;

                if (desc.Contains(t))
                    score += 8;
            }

            // School
            uint school = SafeUInt(row, "SchoolMask");
            if (tokens.Contains("fire") && school == 4) score += 10;
            if (tokens.Contains("frost") && school == 16) score += 10;
            if (tokens.Contains("shadow") && school == 32) score += 10;
            if (tokens.Contains("holy") && school == 2) score += 10;

            // CC
            uint mech = SafeUInt(row, "Mechanic");
            if (tokens.Contains("stun") && mech == 12) score += 10;
            if (tokens.Contains("root") && mech == 7) score += 10;
            if (tokens.Contains("slow") && mech == 11) score += 10;

            return score;
        }

        /// <summary>
        /// Compute a similarity score between two spell rows.
        /// Higher is more similar. We weight:
        /// - Name overlap
        /// - SchoolMask
        /// - Mechanic
        /// - PowerType
        /// - SpellFamilyName
        /// - EffectN
        /// - EffectApplyAuraNameN
        /// - EffectImplicitTargetAN
        /// </summary>
        private static double ComputeSimilarity(DataRow a, DataRow b)
        {
            double score = 0;

            try
            {
                // Name token overlap
                var nameA = (a["SpellName0"] ?? string.Empty).ToString();
                var nameB = (b["SpellName0"] ?? string.Empty).ToString();

                if (!string.IsNullOrWhiteSpace(nameA) && !string.IsNullOrWhiteSpace(nameB))
                {
                    var tokensA = TokenizeName(nameA);
                    var tokensB = TokenizeName(nameB);

                    int overlap = tokensA.Intersect(tokensB).Count();
                    score += overlap * 5; // 5 points per overlapping token
                }

                // School
                uint schoolA = SafeUInt(a, "SchoolMask");
                uint schoolB = SafeUInt(b, "SchoolMask");
                if (schoolA != 0 && schoolA == schoolB)
                    score += 20;

                // Mechanic
                uint mechA = SafeUInt(a, "Mechanic");
                uint mechB = SafeUInt(b, "Mechanic");
                if (mechA != 0 && mechA == mechB)
                    score += 15;

                // PowerType
                uint powerA = SafeUInt(a, "PowerType");
                uint powerB = SafeUInt(b, "PowerType");
                if (powerA == powerB)
                    score += 10;

                // Class family
                uint famA = SafeUInt(a, "SpellFamilyName");
                uint famB = SafeUInt(b, "SpellFamilyName");
                if (famA != 0 && famA == famB)
                    score += 10;

                // Effects / auras / targets for slots 1–3
                for (int i = 1; i <= 3; ++i)
                {
                    string eCol = "Effect" + i;
                    string aCol = "EffectApplyAuraName" + i;
                    string tCol = "EffectImplicitTargetA" + i;

                    uint eA = SafeUInt(a, eCol);
                    uint eB = SafeUInt(b, eCol);
                    if (eA != 0 && eA == eB)
                        score += 10;

                    uint auraA = SafeUInt(a, aCol);
                    uint auraB = SafeUInt(b, aCol);
                    if (auraA != 0 && auraA == auraB)
                        score += 10;

                    uint targetA = SafeUInt(a, tCol);
                    uint targetB = SafeUInt(b, tCol);
                    if (targetA != 0 && targetA == targetB)
                        score += 5;
                }
            }
            catch
            {
                // ignore errors, just return current score
            }

            return score;
        }

        private static IEnumerable<string> TokenizeName(string name)
        {
            return name
                .ToLowerInvariant()
                .Split(new[] { ' ', '\t', '-', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(tok => tok.Length >= 3);
        }

        private static uint SafeUInt(DataRow row, string columnName)
        {
            if (!row.Table.Columns.Contains(columnName))
                return 0;
            var obj = row[columnName];
            if (obj == null || obj == DBNull.Value)
                return 0;
            uint val;
            return uint.TryParse(obj.ToString(), out val) ? val : 0;
        }

        /// <summary>
        /// Builds a human-readable semantic summary of a spell row for the AI.
        /// This is NOT strict JSON, just compact text used inside the prompt.
        /// </summary>
        private static AiSimilarSpellSummary BuildSemanticSummary(DataRow row)
        {
            try
            {
                uint id = SafeUInt(row, "ID");
                string name = (row["SpellName0"] ?? string.Empty).ToString();

                var sb = new StringBuilder();
                sb.AppendLine("Name: " + name);
                sb.AppendLine("ID: " + id);

                // School
                uint schoolMask = SafeUInt(row, "SchoolMask");
                sb.AppendLine("School: " + MapSchoolMaskToName(schoolMask));

                // Mechanic
                uint mech = SafeUInt(row, "Mechanic");
                sb.AppendLine("Mechanic: " + MapMechanicToName(mech));

                // PowerType
                uint power = SafeUInt(row, "PowerType");
                sb.AppendLine("PowerType: " + MapPowerTypeToName(power));

                // Class family
                uint fam = SafeUInt(row, "SpellFamilyName");
                if (fam != 0)
                    sb.AppendLine("ClassFamily: " + MapClassFamilyToName(fam));

                // Range / radius hints (very rough)
                if (row.Table.Columns.Contains("RangeIndex"))
                    sb.AppendLine("RangeIndex: " + SafeUInt(row, "RangeIndex"));

                // Simple guess at single-target vs AoE by target and radius
                sb.AppendLine("Effects:");
                for (int i = 1; i <= 3; ++i)
                {
                    string eCol = "Effect" + i;
                    if (!row.Table.Columns.Contains(eCol))
                        continue;

                    uint effectId = SafeUInt(row, eCol);
                    if (effectId == 0)
                        continue;

                    uint auraId = SafeUInt(row, "EffectApplyAuraName" + i);
                    uint targetId = SafeUInt(row, "EffectImplicitTargetA" + i);
                    int basePoints = 0;
                    if (row.Table.Columns.Contains("EffectBasePoints" + i))
                    {
                        int bp;
                        if (int.TryParse(row["EffectBasePoints" + i].ToString(), out bp))
                            basePoints = bp + 1; // DBC stores BasePoints, real is +1
                    }

                    sb.Append("  - Slot ").Append(i).Append(": ");

                    string guessedType = GuessEffectType(effectId, auraId);
                    string guessedAura = GuessAuraName(auraId);

                    sb.Append("EffectType=").Append(guessedType);
                    if (!string.IsNullOrEmpty(guessedAura))
                        sb.Append(", Aura=").Append(guessedAura);

                    if (basePoints != 0)
                        sb.Append(", Power≈").Append(basePoints);

                    if (targetId != 0)
                        sb.Append(", TargetId=").Append(targetId);

                    sb.AppendLine();
                }

                return new AiSimilarSpellSummary
                {
                    Id = id,
                    Name = name,
                    SummaryText = sb.ToString()
                };
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "BuildSemanticSummary failed");
                return null;
            }
        }

        private static string MapSchoolMaskToName(uint mask)
        {
            switch (mask)
            {
                case 0x01: return "Physical";
                case 0x02: return "Holy";
                case 0x04: return "Fire";
                case 0x08: return "Nature";
                case 0x10: return "Frost";
                case 0x20: return "Shadow";
                case 0x40: return "Arcane";
                default: return "Mixed/Unknown(" + mask + ")";
            }
        }

        private static string MapMechanicToName(uint mech)
        {
            switch (mech)
            {
                case 1: return "Charm";
                case 2: return "Disorient";
                case 3: return "Disarm";
                case 5: return "Fear";
                case 7: return "Root";
                case 9: return "Silence";
                case 10: return "Sleep";
                case 11: return "Snare";
                case 12: return "Stun";
                default: return mech == 0 ? "None" : "Mechanic(" + mech + ")";
            }
        }

        private static string MapPowerTypeToName(uint power)
        {
            switch (power)
            {
                case 0: return "Mana";
                case 1: return "Rage";
                case 2: return "Focus";
                case 3: return "Energy";
                case 4: return "Happiness";
                case 5: return "Runes";
                case 6: return "Runic Power";
                case 0xFFFFFFFE: // -2 cast to uint
                    return "Health";
                default:
                    return "Unknown(" + power + ")";
            }
        }

        private static string MapClassFamilyToName(uint fam)
        {
            switch (fam)
            {
                case 3: return "Mage";
                case 4: return "Warrior";
                case 5: return "Warlock";
                case 6: return "Priest";
                case 7: return "Druid";
                case 8: return "Rogue";
                case 9: return "Hunter";
                case 10: return "Paladin";
                case 11: return "Shaman";
                case 15: return "DeathKnight";
                default: return "Family(" + fam + ")";
            }
        }

        private static string GuessEffectType(uint effectId, uint auraId)
        {
            // Very coarse mapping, just for examples in text.
            // This does NOT affect the actual DBC output.
            switch (effectId)
            {
                case 2: return "Damage";
                case 10: return "Heal";
                case 6: return "ApplyAura";
                default:
                    if (auraId != 0)
                        return "ApplyAura";
                    return "Effect(" + effectId + ")";
            }
        }

        private static string GuessAuraName(uint auraId)
        {
            switch (auraId)
            {
                case 3: return "PeriodicDamage";
                case 8: return "PeriodicHeal";
                case 11: return "ModDecreaseSpeed";
                case 12: return "ModStun";
                default:
                    return auraId == 0 ? "" : "Aura(" + auraId + ")";
            }
        }
    }
}
