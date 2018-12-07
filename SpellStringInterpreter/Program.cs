using System;

namespace SpellStringInterpreter
{
    class Program
    {
        static void Main(string[] args)
        {
            var str = "Causes ${$m1+0.15*$SPH+0.15*$AP} to ${$M1+0.15*$SPH+0.15*$AP} Holy damage to an enemy target";

            str = "Hello test ${5 + 10 + 2 + 1 - 3} and some other formulas ${2.5*5/2} foo bar";

            Console.WriteLine("\n" + str);

            var parser = new SpellStringParser();
            str = parser.ParseString(str);

            Console.WriteLine(str);
            Console.ReadKey();
        }
    }
}
