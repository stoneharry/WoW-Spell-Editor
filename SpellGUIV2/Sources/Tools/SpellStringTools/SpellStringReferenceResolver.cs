using NLog;
using SpellEditor.Sources.DBC;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

namespace SpellEditor.Sources.SpellStringTools
{
    internal class SpellStringReferenceResolver
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static string STR_SECONDS = " seconds";
        private static string STR_INFINITE_DUR = "until cancelled";
        private static string STR_HEARTHSTONE_LOC = "(hearthstone location)";

        private struct TOKEN_TO_PARSER
        {
            public string TOKEN;
            public Func<string, DataRow, MainWindow, string> tokenFunc;
        }

        // Built once, parsing reruns on every keystroke and the static Regex cache only holds 15 patterns
        private static readonly Regex LinkedTargetsRegex = new Regex("\\$([0-9]+)x([1-3])", RegexOptions.Compiled);
        private static readonly Regex LinkedChargesRegex = new Regex("\\$([0-9]+)n", RegexOptions.Compiled);
        private static readonly Regex LinkedPeriodRegex = new Regex("\\$([0-9]+)t([1-3])", RegexOptions.Compiled);
        private static readonly Regex LinkedDurationRegex = new Regex("\\$([0-9]+)d", RegexOptions.Compiled);
        private static readonly Regex LinkedEffectRegex = new Regex("\\$([0-9]+)s([1-3])", RegexOptions.Compiled);
        private static readonly Regex LinkedSpellRegex = new Regex("\\$\\d+", RegexOptions.Compiled);

        private static readonly Dictionary<string, Regex> TokenRegexCache = new Dictionary<string, Regex>();

        private static Regex TokenRegex(string token)
        {
            lock (TokenRegexCache)
            {
                if (!TokenRegexCache.TryGetValue(token, out var regex))
                {
                    regex = new Regex(Regex.Escape(token) + "(?![A-Za-z0-9])", RegexOptions.Compiled);
                    TokenRegexCache[token] = regex;
                }
                return regex;
            }
        }

        // A short token must not eat a longer one, "$b" would otherwise corrupt "$bh"
        private static bool HasToken(string str, string token)
        {
            return str.IndexOf(token, StringComparison.Ordinal) >= 0 && TokenRegex(token).IsMatch(str);
        }

        private static string ReplaceToken(string str, string token, string value)
        {
            if (str.IndexOf(token, StringComparison.Ordinal) < 0)
                return str;
            return TokenRegex(token).Replace(str, value.Replace("$", "$$"));
        }

        private static readonly Dictionary<string, string[]> TokenListCache = new Dictionary<string, string[]>();

        private static string[] Tokens(string tokenList)
        {
            lock (TokenListCache)
            {
                if (!TokenListCache.TryGetValue(tokenList, out var tokens))
                {
                    tokens = tokenList.Split('|');
                    TokenListCache[tokenList] = tokens;
                }
                return tokens;
            }
        }

        private static string FormatRecordValue(object value, string format)
        {
            var raw = value == null ? "0" : value.ToString();
            if (!double.TryParse(raw, out var number))
                return raw;
            return format == null ? number.ToString() : number.ToString(format);
        }

        // Builds a parser for a token with an effect index, like $q1 and $Q1. Both cases read the same field
        private static TOKEN_TO_PARSER IndexedColumnParser(string lower, string upper, string column, string format)
        {
            var tokens = new List<string>();
            for (int i = 1; i <= 3; ++i)
            {
                tokens.Add("$" + lower + i);
                tokens.Add("$" + upper + i);
            }
            var parser = new TOKEN_TO_PARSER { TOKEN = string.Join("|", tokens) };
            parser.tokenFunc = (str, record, mainWindow) =>
            {
                foreach (var token in tokens)
                {
                    var index = token[token.Length - 1];
                    str = ReplaceToken(str, token, FormatRecordValue(record[column + index], format));
                }
                return str;
            };
            return parser;
        }

        private static TOKEN_TO_PARSER SingleColumnParser(string tokenList, string column, string format)
        {
            var tokens = tokenList.Split('|');
            var parser = new TOKEN_TO_PARSER { TOKEN = tokenList };
            parser.tokenFunc = (str, record, mainWindow) =>
            {
                foreach (var token in tokens)
                    str = ReplaceToken(str, token, FormatRecordValue(record[column], format));
                return str;
            };
            return parser;
        }

        private static TOKEN_TO_PARSER rangeParser = new TOKEN_TO_PARSER()
        {
            TOKEN = "$r",
            tokenFunc = (str, record, mainWindow) =>
            {
                if (HasToken(str, rangeParser.TOKEN))
                {
                    var dbc = DBCManager.GetInstance().FindDbcForBinding("SpellRange");
                    if (dbc == null)
                    {
                        Logger.Info("Unable to handle $r spell string token, SpellRange.dbc not loaded");
                        return str;
                    }
                    var rangeDbc = (SpellRange)dbc;
                    foreach (var entry in rangeDbc.Lookups)
                    {
                        var rangeIndex = uint.Parse(record["RangeIndex"].ToString());
                        if (entry.ID == rangeIndex && entry is SpellRange.SpellRangeBoxContainer)
                        {
                            var container = entry as SpellRange.SpellRangeBoxContainer;
                            return ReplaceToken(str, rangeParser.TOKEN, container.RangeString);
                        }
                    }
                }
                return str;
            }
        };

        private static TOKEN_TO_PARSER radiusParser = new TOKEN_TO_PARSER()
        {
            TOKEN = "$a1|$a2|$a3|$A1|$A2|$A3|$a|$A",
            tokenFunc = (str, record, mainWindow) =>
            {
                foreach (var token in Tokens(radiusParser.TOKEN))
                {
                    if (HasToken(str, token))
                    {
                        uint index = 0;
                        if (token.Length == 2)
                        {
                            index = 4;
                        }
                        else
                        {
                            index = uint.Parse(token[2].ToString());
                        }
                        uint radiusVal = 0;
                        if (index == 1)
                        {
                            radiusVal = uint.Parse(record["EffectRadiusIndex1"].ToString());
                        }
                        else if (index == 2)
                        {
                            radiusVal = uint.Parse(record["EffectRadiusIndex2"].ToString());
                        }
                        else if (index == 3)
                        {
                            radiusVal = uint.Parse(record["EffectRadiusIndex3"].ToString());
                        }
                        else if (index == 4)
                        {
                            Logger.Info("Unable to handle $a token in spell string");
                            return str;
                        }
                        var dbc = DBCManager.GetInstance().FindDbcForBinding("SpellRadius");
                        if (dbc == null)
                        {
                            Logger.Info("Unable to handle $a token in spell string, SpellRadius dbc not loaded");
                            return str;
                        }
                        var radiusDbc = (SpellRadius)dbc;
                        for (int i = 0; i < radiusDbc.Lookups.Count; ++i)
                        {
                            if (radiusVal == radiusDbc.Lookups[i].ID)
                            {
                                string item = "";
                                if (index == 1)
                                {
                                    item = mainWindow.RadiusIndex1.Items[radiusDbc.Lookups[i].ComboBoxIndex].ToString();
                                }
                                else if (index == 2)
                                {
                                    item = mainWindow.RadiusIndex2.Items[radiusDbc.Lookups[i].ComboBoxIndex].ToString();
                                }
                                else if (index == 3)
                                {
                                    item = mainWindow.RadiusIndex3.Items[radiusDbc.Lookups[i].ComboBoxIndex].ToString();
                                }
                                str = ReplaceToken(str, token, item.Contains(" ") ? item.Substring(0, item.IndexOf(" ")) : item);
                            }
                        }
                    }
                }
                return str;
            }
        };

        private static TOKEN_TO_PARSER procChanceParser = SingleColumnParser("$h|$H", "ProcChance", null);

        private static TOKEN_TO_PARSER hearthstoneLocationParser = new TOKEN_TO_PARSER()
        {
            TOKEN = "$z",
            tokenFunc = (str, record, mainWindos) =>
            {
                return ReplaceToken(str, hearthstoneLocationParser.TOKEN, STR_HEARTHSTONE_LOC);
            }
        };

        private static TOKEN_TO_PARSER maxTargetLevelParser = SingleColumnParser("$v|$V", "MaximumTargetLevel", null);

        // A spell costing a percentage of base mana shows 0 here
        private static TOKEN_TO_PARSER powerCostParser = SingleColumnParser("$c|$C", "ManaCost", null);

        private static TOKEN_TO_PARSER powerCostPerSecondParser = SingleColumnParser("$p|$P", "ManaPerSecond", null);

        private static TOKEN_TO_PARSER miscValueParser = IndexedColumnParser("q", "Q", "EffectMiscValue", null);

        private static TOKEN_TO_PARSER comboPointsParser = IndexedColumnParser("b", "B", "EffectPointsPerComboPoint", "0.##");

        // $e is EffectMultipleValue, not EffectAmplitude. EffectAmplitude is $t.
        private static TOKEN_TO_PARSER multipleValueParser = IndexedColumnParser("e", "E", "EffectMultipleValue", "0.##");

        private static TOKEN_TO_PARSER damageMultiplierParser = IndexedColumnParser("f", "F", "EffectDamageMultiplier", "0.##");

        // Raw coefficient, in game it is scaled by spell power
        private static TOKEN_TO_PARSER bonusCoefficientParser = IndexedColumnParser("bc", "BC", "EffectBonusMultiplier", "0.###");

        private static TOKEN_TO_PARSER targetsParser = new TOKEN_TO_PARSER()
        {
            TOKEN = "$x1|$x2|$x3|$X1|$X2|$X3|$x|$X",
            tokenFunc = (str, record, mainWindow) =>
            {
                foreach (var token in Tokens(targetsParser.TOKEN))
                {
                    if (HasToken(str, token))
                    {
                        uint index = 0;
                        if (token.Length == 2)
                        {
                            index = 4;
                        }
                        else
                        {
                            index = uint.Parse(token[2].ToString());
                        }
                        uint targetCount = 0;
                        if (index == 1)
                        {
                            targetCount = uint.Parse(record["EffectChainTarget1"].ToString());
                        }
                        else if (index == 2)
                        {
                            targetCount = uint.Parse(record["EffectChainTarget2"].ToString());
                        }
                        else if (index == 3)
                        {
                            targetCount = uint.Parse(record["EffectChainTarget3"].ToString());
                        }
                        else if (index == 4)
                        {
                            targetCount = uint.Parse(record["EffectChainTarget1"].ToString())
                                    + uint.Parse(record["EffectChainTarget2"].ToString())
                                    + uint.Parse(record["EffectChainTarget3"].ToString());
                        }
                        str = ReplaceToken(str, token, targetCount.ToString());
                    }
                }

                MatchCollection _matches = LinkedTargetsRegex.Matches(str);

                foreach (Match _str in _matches)
                {
                    uint _linkId = uint.Parse(_str.Groups[1].Value);
                    uint _index = uint.Parse(_str.Groups[2].Value);

                    DataRow _linkRecord = GetRecordById(_linkId, mainWindow);

                    if (_linkRecord != null && uint.Parse(_linkRecord["ID"].ToString()) != 0)
                    {
                        uint newVal = 0;
                        if (_index == 1)
                        {
                            newVal = uint.Parse(_linkRecord["EffectChainTarget1"].ToString());
                        }
                        else if (_index == 2)
                        {
                            newVal = uint.Parse(_linkRecord["EffectChainTarget2"].ToString());
                        }
                        else if (_index == 3)
                        {
                            newVal = uint.Parse(_linkRecord["EffectChainTarget3"].ToString());
                        }
                        str = str.Replace(_str.ToString(), newVal.ToString());
                    }
                }
                return str;
            }
        };

        private static TOKEN_TO_PARSER summaryDamage = new TOKEN_TO_PARSER()
        {
            TOKEN = "$o1|$o2|$o3|$o",
            tokenFunc = (str, record, mainWindow) =>
            {
                var tokens = Tokens(summaryDamage.TOKEN);
                foreach (var token in tokens)
                {
                    if (HasToken(str, token))
                    {
                        uint index = 0;
                        double cooldown = 0;
                        if (token.Length == 2)
                        {
                            index = 4;
                        }
                        else
                        {
                            index = uint.Parse(token[2].ToString());
                        }
                        int damage = 0;
                        if (index == 1)
                        {
                            damage = int.Parse(record["EffectDieSides1"].ToString()) + int.Parse(record["EffectBasePoints1"].ToString());
                            cooldown = uint.Parse(record["EffectAmplitude1"].ToString()) / 1000;
                        }
                        else if (index == 2)
                        {
                            damage = int.Parse(record["EffectDieSides2"].ToString()) + int.Parse(record["EffectBasePoints2"].ToString());
                            cooldown = uint.Parse(record["EffectAmplitude2"].ToString()) / 1000;
                        }
                        else if (index == 3)
                        {
                            damage = int.Parse(record["EffectDieSides3"].ToString()) + int.Parse(record["EffectBasePoints3"].ToString());
                            cooldown = uint.Parse(record["EffectAmplitude3"].ToString()) / 1000;
                        }
                        else if (index == 4)
                        {
                            damage = int.Parse(record["EffectDieSides1"].ToString()) + int.Parse(record["EffectBasePoints1"].ToString()) +
                                    int.Parse(record["EffectDieSides2"].ToString()) + int.Parse(record["EffectBasePoints2"].ToString()) +
                                    int.Parse(record["EffectDieSides3"].ToString()) + int.Parse(record["EffectBasePoints3"].ToString());
                            cooldown = (uint.Parse(record["EffectAmplitude1"].ToString()) +
                                        uint.Parse(record["EffectAmplitude2"].ToString()) +
                                        uint.Parse(record["EffectAmplitude3"].ToString())) / 1000;
                        }
                        var entry = DBCManager.GetInstance().FindDbcForBinding("SpellDuration").LookupRecord(uint.Parse(record["DurationIndex"].ToString()));
                        if (entry != null)
                        {
                            string newStr;
                            int baseDuration = int.Parse(entry["BaseDuration"].ToString());
                            // Convert duration to seconds
                            if (baseDuration == -1)
                                newStr = STR_INFINITE_DUR;
                            else
                            {
                                var seconds = double.Parse(baseDuration.ToString()) / 1000;
                                var total = damage * (seconds / cooldown);
                                newStr = total.ToString();
                            }
                            str = ReplaceToken(str, token, newStr);
                        }
                    }
                }
                return str;
            }
        };

        private static TOKEN_TO_PARSER procChargesParser = new TOKEN_TO_PARSER()
        {
            TOKEN = "$n|$N",
            tokenFunc = (str, record, mainWindow) =>
            {
                foreach (var token in Tokens(procChargesParser.TOKEN))
                {
                    str = ReplaceToken(str, token, record["ProcCharges"].ToString());
                }

                MatchCollection _matches = LinkedChargesRegex.Matches(str);

                foreach (Match _str in _matches)
                {
                    uint _LinkId = uint.Parse(_str.Groups[1].Value);
                    DataRow _linkRecord = GetRecordById(_LinkId, mainWindow);

                    if (_linkRecord != null && uint.Parse(_linkRecord["ID"].ToString()) != 0)
                    {
                        str = str.Replace(_str.ToString(), _linkRecord["ProcCharges"].ToString());
                    }
                }

                return str;
            }
        };

        private static TOKEN_TO_PARSER stackParser = SingleColumnParser("$u|$U", "StackAmount", null);

        private static TOKEN_TO_PARSER periodicTriggerParser = new TOKEN_TO_PARSER()
        {
            TOKEN = "$t1|$t2|$t3|$T1|$T2|$T3|$t|$T",
            tokenFunc = (str, record, mainWindow) =>
            {
                var tokens = Tokens(periodicTriggerParser.TOKEN);
                foreach (var token in tokens)
                {
                    if (HasToken(str, token))
                    {
                        uint index = 0;
                        if (token.Length == 2)
                        {
                            index = 4;
                        }
                        else
                        {
                            index = uint.Parse(token[2].ToString());
                        }
                        uint newVal = 0;
                        if (index == 1)
                        {
                            newVal = uint.Parse(record["EffectAmplitude1"].ToString());
                        }
                        else if (index == 2)
                        {
                            newVal = uint.Parse(record["EffectAmplitude2"].ToString());
                        }
                        else if (index == 3)
                        {
                            newVal = uint.Parse(record["EffectAmplitude3"].ToString());
                        }
                        else if (index == 4)
                        {
                            newVal = uint.Parse(record["EffectAmplitude1"].ToString()) +
                                    uint.Parse(record["EffectAmplitude2"].ToString()) +
                                    uint.Parse(record["EffectAmplitude3"].ToString());
                        }
                        var singleVal = Single.Parse(newVal.ToString());
                        str = ReplaceToken(str, token, (singleVal / 1000).ToString());
                    }
                }

                MatchCollection _matches = LinkedPeriodRegex.Matches(str);

                foreach (Match _str in _matches)
                {
                    uint _linkId = uint.Parse(_str.Groups[1].Value);
                    uint _index = uint.Parse(_str.Groups[2].Value);
                    DataRow _linkRecord = GetRecordById(_linkId, mainWindow);

                    if (_linkRecord != null && uint.Parse(_linkRecord["ID"].ToString()) != 0)
                    {
                        uint newVal = 0;
                        if (_index == 1)
                        {
                            newVal = uint.Parse(_linkRecord["EffectAmplitude1"].ToString());
                        }
                        else if (_index == 2)
                        {
                            newVal = uint.Parse(_linkRecord["EffectAmplitude2"].ToString());
                        }
                        else if (_index == 3)
                        {
                            newVal = uint.Parse(_linkRecord["EffectAmplitude3"].ToString());
                        }
                        var singleVal = float.Parse(newVal.ToString());
                        str = str.Replace(_str.ToString(), (singleVal / 1000).ToString());
                    }
                }
                return str;
            }
        };

        private static TOKEN_TO_PARSER durationParser = new TOKEN_TO_PARSER()
        {
            TOKEN = "$d|$D",
            tokenFunc = (str, record, mainWindow) =>
            {
                if (HasToken(str, "$d") || HasToken(str, "$D"))
                {
                    var entry = DBCManager.GetInstance().FindDbcForBinding("SpellDuration").LookupRecord(uint.Parse(record["DurationIndex"].ToString()));
                    if (entry != null)
                    {
                        string newStr;
                        uint baseDuration = uint.Parse(entry["BaseDuration"].ToString());
                        // Convert duration to seconds
                        if (baseDuration == uint.MaxValue)
                            newStr = STR_INFINITE_DUR;
                        else
                        {
                            var seconds = float.Parse(baseDuration.ToString()) / 1000f;
                            newStr = seconds + STR_SECONDS;
                        }
                        foreach (var token in Tokens(durationParser.TOKEN))
                            str = ReplaceToken(str, token, newStr);
                    }
                }

                //Handling strings similar to "$1510d" (spell:1510)
                MatchCollection _matches = LinkedDurationRegex.Matches(str);

                foreach (Match _str in _matches)
                {
                    uint _LinkId = uint.Parse(_str.Groups[1].Value);
                    DataRow _linkRecord = GetRecordById(_LinkId, mainWindow);
                    if (_linkRecord != null && uint.Parse(_linkRecord["ID"].ToString()) != 0)
                    {
                        var entry = DBCManager.GetInstance().FindDbcForBinding("SpellDuration").LookupRecord(uint.Parse(_linkRecord["DurationIndex"].ToString()));
                        if (entry != null)
                        {
                            string newStr;
                            int baseDuration = int.Parse(entry["BaseDuration"].ToString());
                            // Convert duration to seconds
                            if (baseDuration == -1)
                                newStr = STR_INFINITE_DUR;
                            else
                            {
                                var seconds = float.Parse(baseDuration.ToString()) / 1000f;
                                newStr = seconds + STR_SECONDS;
                            }
                            str = str.Replace(_str.ToString(), newStr);
                        }
                    }
                }
                return str;
            }
        };

        private static TOKEN_TO_PARSER spellEffectParser = new TOKEN_TO_PARSER()
        {
            TOKEN = "$s1|$s2|$s3|$s",
            tokenFunc = (str, record, mainWindow) =>
            {
                var tokens = Tokens(spellEffectParser.TOKEN);

                foreach (var token in tokens)
                {
                    if (HasToken(str, token))
                    {
                        var index = 0;
                        if (token.Length == 2)
                        {
                            index = 4;
                        }
                        else
                        {
                            index = int.Parse(token[2].ToString());
                        }
                        string newVal = "0";
                        if (index >= 1 && index <= 3)
                        {
                            var dieSides = int.Parse(record["EffectDieSides" + index].ToString());
                            if (dieSides == 0 || dieSides == 1)
                            {
                                newVal = (int.Parse(record["EffectBasePoints" + index].ToString()) + dieSides).ToString();
                            }
                            else
                            {
                                var basePoints = int.Parse(record["EffectBasePoints" + index].ToString());
                                newVal = (basePoints + 1) + " to " + (basePoints + dieSides);
                            }
                        }
                        else if (index == 4)
                        {
                            var sum = 0;
                            for (int i = 1; i <= 3; ++i)
                            {
                                sum += int.Parse(record["EffectBasePoints" + i].ToString()) + int.Parse(record["EffectDieSides" + i].ToString());
                            }
                            newVal = sum.ToString();
                        }
                        // Negative values are actually shown positive
                        // 'reduces targets movement speed by 50%'
                        // The 50% has a value of -50 but is shown as 50
                        if (int.TryParse(newVal, out var intVal) && intVal < 0)
                        {
                            newVal = (intVal *= -1).ToString();
                        }

                        str = ReplaceToken(str, token, newVal);
                    }
                }

                MatchCollection _matches = LinkedEffectRegex.Matches(str);

                foreach (Match _str in _matches)
                {
                    uint _linkId = uint.Parse(_str.Groups[1].Value);
                    uint _index = uint.Parse(_str.Groups[2].Value);

                    DataRow _linkRecord = GetRecordById(_linkId, mainWindow);

                    if (_linkRecord != null && uint.Parse(_linkRecord["ID"].ToString()) != 0)
                    {
                        int newVal = 0;
                        if (_index >= 1 && _index <= 3)
                        {
                            newVal = int.Parse(record["EffectBasePoints" + _index].ToString()) +
                                    int.Parse(record["EffectDieSides" + _index].ToString());
                        }
                        str = str.Replace(_str.ToString(), newVal.ToString());
                    }
                }
                return str;
            }
        };

        private static TOKEN_TO_PARSER maxTargetHandler = SingleColumnParser("$i|$I", "MaximumAffectedTargets", null);

        // Lower case is the minimum effect points, upper case the maximum
        private static TOKEN_TO_PARSER effectPointsParser = new TOKEN_TO_PARSER()
        {
            TOKEN = "$m1|$m2|$m3|$M1|$M2|$M3",
            tokenFunc = (str, record, mainWindow) =>
            {
                foreach (var token in Tokens(effectPointsParser.TOKEN))
                {
                    var index = token[token.Length - 1].ToString();
                    var basePoints = int.Parse(record["EffectBasePoints" + index].ToString());
                    var dieSides = int.Parse(record["EffectDieSides" + index].ToString());
                    var value = char.IsUpper(token[1]) ? basePoints + dieSides : basePoints + 1;
                    // Negative values read positive in a description, "reduces speed by 50%" is -50
                    str = ReplaceToken(str, token, Math.Abs(value).ToString());
                }
                return str;
            }
        };

        private static TOKEN_TO_PARSER knownUnhandledTokenParser = new TOKEN_TO_PARSER()
        {
            // These need a live player to resolve, so the editor shows zero
            /*
             * $STR $AGI $STA $INT $SPI    Effective stat, buffs included
             * $str $agi $sta $int $spi    Base stat
             * $ap $AP                     Melee attack power
             * $rap $RAP                   Ranged attack power
             * $mwb $MWB                   Main hand weapon base damage, min and max
             * $owb $OWB                   Off hand weapon base damage, min and max
             * $rwb $RWB                   Ranged weapon base damage, min and max
             * $mw $MW                     Main hand weapon damage, min and max
             * $ow $OW                     Off hand weapon damage, min and max
             * $rw $RW                     Ranged weapon damage, min and max
             * $ar $AR                     Armor, buffs included
             * $mws $MWS $ows $OWS         Main hand and off hand weapon speed in seconds
             * $rws $RWS                   Ranged weapon speed in seconds
             * $pl $PL                     Player level
             * $hnd $HND                   Handedness
             * $sp $SP                     Spell power
             * $sph $spfi $spn $spfr $sps $spa   Spell power by school, upper case forms too
             * $bh $BH                     Bonus healing
             * $ph $pfi $pn $pfr $ps $pa   Percent damage done modifier by school, upper case too
             * $pbh $pBH $pbhd $pBHD       Pet bonus healing
             * $b $B                       Unindexed form only, $b1-3 is points per combo point
            */
            TOKEN = "$STR|$AGI|$STA|$INT|$SPI|$str|$agi|$sta|$int|$spi|" +
                    "$ap|$AP|$rap|$RAP|" +
                    "$mwb|$MWB|$owb|$OWB|$rwb|$RWB|" +
                    "$mw|$MW|$ow|$OW|$rw|$RW|" +
                    "$ar|$AR|" +
                    "$mws|$MWS|$ows|$OWS|$rws|$RWS|" +
                    "$pl|$PL|$hnd|$HND|$sp|$SP|" +
                    "$sph|$spfi|$spn|$spfr|$sps|$spa|$SPH|$SPFI|$SPN|$SPFR|$SPS|$SPA|" +
                    "$bh|$BH|" +
                    "$ph|$pfi|$pn|$pfr|$ps|$pa|$PH|$PFI|$PN|$PFR|$PS|$PA|" +
                    "$pbh|$pBH|$pbhd|$pBHD|" +
                    "$b|$B",
            tokenFunc = (str, record, mainWindow) =>
            {
                foreach (var token in Tokens(knownUnhandledTokenParser.TOKEN))
                {
                    str = ReplaceToken(str, token, "0");
                }
                return str;
            }
        };

        // "Causes ${$m1+0.15*$SPH+0.15*$AP} to ${$M1+0.15*$SPH+0.15*$AP} Holy damage to an enemy target"
        private static readonly TOKEN_TO_PARSER[] TOKEN_PARSERS = {
            knownUnhandledTokenParser, // Should be first to ensure unknowns are swapped out and other tokens can be resolved
            procChanceParser,
            spellEffectParser,
            durationParser,
            procChargesParser,
            periodicTriggerParser,
            summaryDamage,
            targetsParser,
            maxTargetLevelParser,
            hearthstoneLocationParser,
            radiusParser,
            rangeParser,
            stackParser,
            maxTargetHandler,
            effectPointsParser,
            powerCostParser,
            powerCostPerSecondParser,
            miscValueParser,
            comboPointsParser,
            multipleValueParser,
            damageMultiplierParser,
            bonusCoefficientParser
        };

        public static string GetParsedForm(string rawString, DataRow record, MainWindow mainWindow)
        {
            if (string.IsNullOrEmpty(rawString) || rawString.IndexOf('$') < 0)
                return rawString;

            // If a token starts with $ and a number, it references that as a spell id
            var match = LinkedSpellRegex.Match(rawString);
            if (match.Success)
            {
                if (!uint.TryParse(match.Value.Substring(1), out uint otherId))
                {
                    Logger.Info("Failed to parse other spell id: " + rawString);
                    return rawString;
                }
                var otherRecord = SpellDBC.GetRecordById(otherId, mainWindow);
                if (otherRecord == null)
                    return rawString;
                int offset = match.Index + match.Value.Length;
                bool hasPrefix = rawString.StartsWith("$");
                rawString = rawString.Substring(match.Index + match.Value.Length);
                if (hasPrefix)
                    rawString = "$" + rawString;
                foreach (TOKEN_TO_PARSER parser in TOKEN_PARSERS)
                    rawString = parser.tokenFunc(rawString, otherRecord, mainWindow);
                return rawString;
            }
            foreach (TOKEN_TO_PARSER parser in TOKEN_PARSERS)
                rawString = parser.tokenFunc(rawString, record, mainWindow);
            return rawString;
        }

        public static DataRow GetRecordById(uint spellId, MainWindow mainWindow)
        {
            return SpellDBC.GetRecordById(spellId, mainWindow);
        }
    }
}
