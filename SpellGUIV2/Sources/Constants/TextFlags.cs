using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.Constants
{
    enum TextFlags : uint
    {
        EMPTY       = 0xFF01FC,
        NOT_EMPTY   = 0xFF01FE,
        // Schlumpf: found a localization flag not being one of the two you know but 0xFF01CC,
        // making bit 2 the "is present" bit and adding bits 5 and 6 to the set of unknown ones.
        UNKNOWN_1   = 0xFF01CC,
        UNKNOWN_2   = 0xFF01EE
    }
}
