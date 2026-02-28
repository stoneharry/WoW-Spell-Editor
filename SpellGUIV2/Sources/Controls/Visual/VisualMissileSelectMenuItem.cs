using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SpellEditor.Sources.Controls.Visual
{
    class VisualMissileSelectMenuItem : MenuItem
    {
        public readonly string VisualId;
        public readonly string MissileId;

        public VisualMissileSelectMenuItem(string visualId, string missileId)
        {
            VisualId = visualId;
            MissileId = missileId;
            Header = TryFindResource("VisualMissileSelect") ?? "Select missile (saves immediately)";
        }
    }
}
