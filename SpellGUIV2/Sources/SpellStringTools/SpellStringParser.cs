using SpellEditor.Sources.DBC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Text.RegularExpressions;
using SpellEditor.Sources.Config;

namespace SpellEditor.Sources.SpellStringTools
{
    class SpellStringParser
    {
        private static string STR_SECONDS = " seconds";
        private static string STR_INFINITE_DUR = "permanently";
        private static string STR_HEARTHSTONE_LOC = "(hearthstone location)";

        private struct TOKEN_TO_PARSER
        {
            public string TOKEN;
            public Func<string, Spell_DBC_Record, MainWindow, string> tokenFunc;
        }

        private static TOKEN_TO_PARSER hearthstoneLocationParser = new TOKEN_TO_PARSER()
        {
            TOKEN = "$z",
            tokenFunc = (str, record,mainWindosw) =>
            {
                if (str.Contains(hearthstoneLocationParser.TOKEN))
                {
                    str = str.Replace(hearthstoneLocationParser.TOKEN, STR_HEARTHSTONE_LOC);
                }
                return str;
            }
        };

        private static TOKEN_TO_PARSER maxTargetLevelParser = new TOKEN_TO_PARSER()
        {
            TOKEN = "$v",
            tokenFunc = (str, record,mainWindosw) =>
            {
                if (str.Contains(maxTargetLevelParser.TOKEN))
                {
                    str = str.Replace(maxTargetLevelParser.TOKEN, record.MaximumTargetLevel.ToString());
                }
                return str;
            }
        };

        private static TOKEN_TO_PARSER targetsParser = new TOKEN_TO_PARSER()
        {
            TOKEN = "$x1|$x2|$x3|$x",
            tokenFunc = (str, record,mainWindosw) =>
            {
                foreach (var token in targetsParser.TOKEN.Split('|'))
                {
                    if (str.Contains(token))
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
                            targetCount = record.EffectChainTarget1;
                        }
                        else if (index == 2)
                        {
                            targetCount = record.EffectChainTarget2;
                        }
                        else if (index == 3)
                        {
                            targetCount = record.EffectChainTarget3;
                        }
                        else if (index == 4)
                        {
                            targetCount = record.EffectChainTarget1
                                    + record.EffectChainTarget2
                                    + record.EffectChainTarget3;
                        }
                        str = str.Replace(token, targetCount.ToString());
                    }
                }
                return str;
            }
        };

        private static TOKEN_TO_PARSER summaryDamage = new TOKEN_TO_PARSER()
        {
            TOKEN = "$o1|$o2|$o3|$o",
            tokenFunc = (str, record,mainWindosw) =>
            {
            var tokens = summaryDamage.TOKEN.Split('|');
            foreach (var token in tokens)
            {
                    if (str.Contains(token))
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
                            damage = record.EffectDieSides1 + record.EffectBasePoints1;
                            cooldown = record.EffectAmplitude1 / 1000;
                        }
                        else if (index == 2)
                        {
                            damage = record.EffectDieSides2 + record.EffectBasePoints2;
                            cooldown = record.EffectAmplitude2 / 1000;
                        }
                        else if (index == 3)
                        {
                            damage = record.EffectDieSides3 + record.EffectBasePoints3;
                            cooldown = record.EffectAmplitude3 / 1000;
                        }
                        else if (index == 4)
                        {
                            damage = (record.EffectDieSides1 + record.EffectBasePoints1) + 
                                    (record.EffectDieSides2 + record.EffectBasePoints2) +
                                    (record.EffectDieSides3 + record.EffectBasePoints3);
                            cooldown = (record.EffectAmplitude1 +
                                        record.EffectAmplitude2 +
                                        record.EffectAmplitude3) / 1000;
                        }
                        foreach (SpellDuration.SpellDurationRecord durRec in SpellDuration.body.records)
                        {
                            if (durRec.ID == record.DurationIndex)
                            {
                                string newStr;
                                // Convert duration to seconds
                                if (durRec.BaseDuration == -1)
                                {
                                    newStr = STR_INFINITE_DUR;
                                }
                                else
                                {
                                    var seconds = double.Parse(durRec.BaseDuration.ToString()) / 1000;
                                    var total = damage * (seconds / cooldown);
                                    newStr = total.ToString();
                                }
                                str = str.Replace(token, newStr);
                            }
                        }
                    }
                }
                return str;
            }
        };

        private static TOKEN_TO_PARSER stacksParser = new TOKEN_TO_PARSER()
        {
            TOKEN = "$n",
            tokenFunc = (str, record,mainWindosw) =>
            {
                if (str.Contains(stacksParser.TOKEN))
                {
                    str = str.Replace(stacksParser.TOKEN, record.ProcCharges.ToString());
                }
                return str;
            }
        };

        private static TOKEN_TO_PARSER periodicTriggerParser = new TOKEN_TO_PARSER()
        {
            TOKEN = "$t1|$t2|$t3|$t",
            tokenFunc = (str, record,mainWindosw) =>
            {
                var tokens = periodicTriggerParser.TOKEN.Split('|');
                foreach (var token in tokens)
                {
                    if (str.Contains(token))
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
                            newVal = record.EffectAmplitude1;
                        }
                        else if (index == 2)
                        {
                            newVal = record.EffectAmplitude2;
                        }
                        else if (index == 3)
                        {
                            newVal = record.EffectAmplitude3;
                        }
                        else if (index == 4)
                        {
                            newVal = record.EffectAmplitude1 + record.EffectAmplitude2 + record.EffectAmplitude3;
                        }
                        var singleVal = Single.Parse(newVal.ToString());
                        str = str.Replace(token, (singleVal / 1000).ToString());
                    }
                }
                return str;
            }
        };

        private static TOKEN_TO_PARSER durationParser = new TOKEN_TO_PARSER()
        {
            TOKEN = "$d",
            tokenFunc = (str, record,mainWindos) =>
            {
                if (str.Contains(durationParser.TOKEN))
                {
                    foreach (SpellDuration.SpellDurationRecord durRec in SpellDuration.body.records)
                    {
                        if (durRec.ID == record.DurationIndex)
                        {
                            string newStr;
                            // Convert duration to seconds
                            if (durRec.BaseDuration == -1)
                            {
                                newStr = STR_INFINITE_DUR;
                            }
                            else
                            {
                                var seconds = Single.Parse(durRec.BaseDuration.ToString()) / 1000f;
                                newStr = seconds + STR_SECONDS;
                            }
                            str = str.Replace(durationParser.TOKEN, newStr);
                        }
                    }
                }

				//Handling strings similar to "$1510d" (spell:1510)
				Match _str = Regex.Match(str, "\\$([0-9]+)d");
				if (_str.Success)
				{
					UInt32 _LinkId =  UInt32.Parse(_str.Groups[1].Value);

					//todo: need add function for find Spell_DBC_Record by id
					//Using database queries or stored in memory and find in memory??

					Spell_DBC_Record _linkRecord = GetRecordById(_LinkId,mainWindos);

					if (_linkRecord.ID != 0)
					{
						foreach (SpellDuration.SpellDurationRecord durRec in SpellDuration.body.records)
						{
							if (durRec.ID == _linkRecord.DurationIndex)
							{
								string newStr;
								// Convert duration to seconds
								if (durRec.BaseDuration == -1)
								{
									newStr = STR_INFINITE_DUR;
								}
								else
								{
									var seconds = Single.Parse(durRec.BaseDuration.ToString()) / 1000f;
									newStr = seconds + STR_SECONDS;
								}
								str = str.Replace(_str.ToString(), newStr);
							}
						}
					}
				}
                return str;
            }
        };

        private static TOKEN_TO_PARSER spellEffectParser = new TOKEN_TO_PARSER()
        {
            TOKEN = "$s1|$s2|$s3|$s",
            tokenFunc = (str, record,mainWindosw) =>
            {
                var tokens = spellEffectParser.TOKEN.Split('|');

                foreach (var token in tokens)
                {
                    if (str.Contains(token))
                    {
                        var index = 0;
                        if (token.Length == 2)
                        {
                            index = 4;
                        }
                        else
                        {
                            index = Int32.Parse(token[2].ToString());
                        }
                        int newVal = 0;
                        if (index == 1)
                        {
                            newVal = record.EffectBasePoints1 + record.EffectDieSides1;
                        }
                        else if (index == 2)
                        {
                            newVal = record.EffectBasePoints2 + record.EffectDieSides2;
                        }
                        else if (index == 3)
                        {
                            newVal = record.EffectBasePoints3 + record.EffectDieSides3;
                        }
                        else if (index == 4)
                        {
                            newVal = record.EffectBasePoints1 + record.EffectDieSides1
                                    + record.EffectBasePoints2 + record.EffectDieSides2
                                    + record.EffectBasePoints3 + record.EffectDieSides3;
                        }
                        str = str.Replace(token, newVal.ToString());
                    }
                }

                return str;
            }
        };

        // "Causes ${$m1+0.15*$SPH+0.15*$AP} to ${$M1+0.15*$SPH+0.15*$AP} Holy damage to an enemy target"

        private static TOKEN_TO_PARSER[] TOKEN_PARSERS = {
            spellEffectParser, durationParser, stacksParser,
            periodicTriggerParser, summaryDamage, targetsParser,
            maxTargetLevelParser, hearthstoneLocationParser
        };

		public static string GetParsedForm(string rawString, Spell_DBC_Record record, MainWindow mainWindow)
        {
            foreach (TOKEN_TO_PARSER parser in TOKEN_PARSERS)
            {
                rawString = parser.tokenFunc(rawString, record, mainWindow);
            }
            return rawString;
        }

		public static string GetParsedForm(string rawString, DataRow row, MainWindow mainWindow)
        {
            Spell_DBC_Record record = SpellDBC.GetRowToRecord(row);
            return GetParsedForm(rawString, record,mainWindow);
        }

		public static Spell_DBC_Record GetRecordById(UInt32 spellId, MainWindow mainWindow)
		{
			return SpellDBC.GetRecordById(spellId, mainWindow);
		}
    }
}
