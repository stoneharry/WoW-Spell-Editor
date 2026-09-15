using SpellEditor.Sources.DBC;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace SpellEditor.Sources.SpellStringTools
{
    // A ${} formula worked out the way the game does. Precedence is ^ then * / % then + -, all
    // left associative, and functions carry the dollar: ${$abs($m1)}, not ${abs($m1)}.
    public static class SpellFormula
    {
        // A token runs until one of these or the end of the formula
        private const string Delimiters = "()[]^*/%+-#,> ";

        // Name and argument count, matched case insensitively
        private static readonly Dictionary<string, int> Functions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "abs", 1 },
            { "ceil", 1 },
            { "floor", 1 },
            { "min", 2 },
            { "max", 2 },
            { "gt", 2 },
            { "lt", 2 },
            { "gte", 2 },
            { "lte", 2 },
            { "eq", 2 },
            { "cond", 3 },
            { "clamp", 3 }
        };

        // What $eq compares the difference against
        private const double EqualEpsilon = 0.000099999997;

        public static bool TryEvaluate(string body, DataRow record, MainWindow mainWindow, out double value, out string error)
        {
            value = 0;
            error = null;
            try
            {
                var parser = new Parser(Tokenize(body), record, mainWindow);
                value = parser.Evaluate();
                return true;
            }
            catch (FormulaException ex)
            {
                error = ex.Message;
                return false;
            }
        }

        // A whole number, or that many decimals when the closing brace is followed by .1 to .9.
        public static string Format(double value, int decimals)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return value.ToString(CultureInfo.InvariantCulture);
            return Math.Round(value, decimals, MidpointRounding.ToEven)
                .ToString("F" + decimals, CultureInfo.InvariantCulture);
        }

        private static List<string> Tokenize(string body)
        {
            var tokens = new List<string>();
            var at = 0;
            while (at < body.Length)
            {
                var c = body[at];
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
                {
                    ++at;
                    continue;
                }
                if (Delimiters.IndexOf(c) >= 0)
                {
                    tokens.Add(c.ToString());
                    ++at;
                    continue;
                }
                var start = at;
                while (at < body.Length && Delimiters.IndexOf(body[at]) < 0 && body[at] != '\t' && body[at] != '\r' && body[at] != '\n')
                    ++at;
                tokens.Add(body.Substring(start, at - start));
            }
            return tokens;
        }

        private class FormulaException : Exception
        {
            public FormulaException(string message) : base(message)
            {
            }
        }

        private class Parser
        {
            private readonly List<string> _tokens;
            private readonly MainWindow _mainWindow;
            private DataRow _record;
            private int _index;

            public Parser(List<string> tokens, DataRow record, MainWindow mainWindow)
            {
                _tokens = tokens;
                _record = record;
                _mainWindow = mainWindow;
            }

            public double Evaluate()
            {
                if (_tokens.Count == 0)
                    throw new FormulaException("empty formula");
                var value = Additive();
                if (_index < _tokens.Count)
                    throw new FormulaException($"unexpected '{_tokens[_index]}'");
                return value;
            }

            private double Additive()
            {
                var value = Multiplicative();
                while (true)
                {
                    var op = Peek();
                    if (op == "+")
                    {
                        ++_index;
                        value += Multiplicative();
                    }
                    else if (op == "-")
                    {
                        ++_index;
                        value -= Multiplicative();
                    }
                    else
                    {
                        return value;
                    }
                }
            }

            private double Multiplicative()
            {
                var value = Power();
                while (true)
                {
                    var op = Peek();
                    if (op == "*")
                    {
                        ++_index;
                        value *= Power();
                    }
                    else if (op == "/")
                    {
                        ++_index;
                        value /= Power();
                    }
                    else if (op == "%")
                    {
                        ++_index;
                        value = Modulo(value, Power());
                    }
                    else
                    {
                        return value;
                    }
                }
            }

            private double Power()
            {
                var value = Element();
                while (Peek() == "^")
                {
                    ++_index;
                    value = Math.Pow(value, Element());
                }
                return value;
            }

            private double Element()
            {
                var token = Next();
                if (token == "(")
                {
                    var value = Additive();
                    Expect(")");
                    return value;
                }
                // A leading minus works in game, a leading plus does not.
                if (token == "-")
                    return -Element();
                if (token[0] == '$')
                    return Variable(token);
                if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                    return number;
                // The easy mistake, a function is only a function after a dollar
                if (Functions.ContainsKey(token))
                    throw new FormulaException($"functions take the dollar too, write ${token}(...)");
                if (SpellFormulaVariables.IsKnown(token))
                    throw new FormulaException($"'{token}' is missing its dollar, write ${token}");
                throw new FormulaException($"'{token}' is not a number");
            }

            private double Variable(string token)
            {
                var name = token.Substring(1);
                if (name.StartsWith("<", StringComparison.Ordinal))
                {
                    // A SpellDescriptionVariables entry, which ParseString already zeroed out.
                    Expect(">");
                    return 0;
                }
                if (name.Length == 0)
                    throw new FormulaException("a lone dollar sign is not a variable");
                if (Functions.TryGetValue(name, out var argumentCount))
                    return Function(name, argumentCount);

                var digits = 0;
                while (digits < name.Length && char.IsDigit(name[digits]))
                    ++digits;
                if (digits > 0)
                {
                    // $<id><variable> switches spell for the rest of the formula.
                    if (!uint.TryParse(name.Substring(0, digits), out var spellId))
                        throw new FormulaException($"'{name}' is not a spell id");
                    var other = SpellDBC.GetRecordById(spellId, _mainWindow);
                    if (other == null)
                        throw new FormulaException($"spell {spellId} does not exist");
                    _record = other;
                    name = name.Substring(digits);
                    if (name.Length == 0)
                        throw new FormulaException($"spell {spellId} is missing a variable after it");
                }

                if (!SpellFormulaVariables.TryResolve(name, _record, out var value))
                    throw new FormulaException($"'{name}' is not a formula variable");
                return value;
            }

            private double Function(string name, int argumentCount)
            {
                Expect("(");
                var arguments = new double[argumentCount];
                arguments[0] = Additive();
                for (int i = 1; i < argumentCount; ++i)
                {
                    Expect(",");
                    arguments[i] = Additive();
                }
                Expect(")");
                return Apply(name, arguments);
            }

            private static double Apply(string name, double[] arguments)
            {
                switch (name.ToLowerInvariant())
                {
                    case "abs": return Math.Abs(arguments[0]);
                    case "ceil": return Math.Ceiling(arguments[0]);
                    case "floor": return Math.Floor(arguments[0]);
                    case "min": return Math.Min(arguments[0], arguments[1]);
                    case "max": return Math.Max(arguments[0], arguments[1]);
                    case "gt": return arguments[0] > arguments[1] ? 1 : 0;
                    case "lt": return arguments[0] < arguments[1] ? 1 : 0;
                    case "gte": return arguments[0] >= arguments[1] ? 1 : 0;
                    case "lte": return arguments[0] <= arguments[1] ? 1 : 0;
                    case "eq": return Math.Abs(arguments[0] - arguments[1]) < EqualEpsilon ? 1 : 0;
                    case "cond": return arguments[0] != 0 ? arguments[1] : arguments[2];
                    case "clamp": return Math.Min(arguments[2], Math.Max(arguments[0], arguments[1]));
                }
                throw new FormulaException($"'{name}' is not a function");
            }

            // The game's % is not a remainder, it rounds left down to a multiple of right, so
            // ${7%3} comes out as 6 and not 1.
            private static double Modulo(double left, double right)
            {
                var quotient = left / right;
                return left - (quotient - Math.Floor(quotient)) * right;
            }

            private string Peek()
            {
                return _index < _tokens.Count ? _tokens[_index] : null;
            }

            private string Next()
            {
                if (_index >= _tokens.Count)
                    throw new FormulaException("the formula ends early");
                return _tokens[_index++];
            }

            private void Expect(string token)
            {
                var next = Peek();
                if (next != token)
                    throw new FormulaException($"expected '{token}'" + (next == null ? "" : $" but found '{next}'"));
                ++_index;
            }
        }
    }
}
