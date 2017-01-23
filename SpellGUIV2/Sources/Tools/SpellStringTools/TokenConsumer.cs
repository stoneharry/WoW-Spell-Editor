using SpellEditor.Sources.DBC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.SpellStringTools
{
    class TokenConsumer
    {
        private string str;
        private Spell_DBC_Record record;

        public TokenConsumer(string rawString, Spell_DBC_Record record)
        {
            this.str = rawString;
            this.record = record;
        }

        public Token Consume()
        {
            Token token = new Token();

            return token;
        }
    }
}
