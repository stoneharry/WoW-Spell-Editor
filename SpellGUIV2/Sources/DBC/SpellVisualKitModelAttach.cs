using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.DBC
{
    class SpellVisualKitModelAttach : AbstractDBC
    {
        public SpellVisualKitModelAttach()
        {
            ReadDBCFile(Config.Config.DbcDirectory + "\\SpellVisualKitModelAttach.dbc");
        }

        public List<Dictionary<string, object>> LookupRecords(uint parentKitId)
        {
            var matches = new List<Dictionary<string, object>>();
            foreach (var record in Body.RecordMaps)
            {
                var parentId = uint.Parse(record["ParentSpellVisualKitId"].ToString());
                if (parentId == parentKitId)
                {
                    matches.Add(record);
                }
            }
            return matches;
        }

        public string LookupAttachmentIndex(int index) => Enum.GetName(typeof(AttachmentPoint), index);

        public enum AttachmentPoint
        {
            Shield,
            HandRight,
            HandLeft,
            ElbowRight,
            ElbowLeft,
            ShoulderRight,
            ShoulderLeft,
            KneeRight,
            KneeLeft,
            HipRight,
            HipLeft,
            Helm,
            Back,
            ShoulderFlapRight,
            ShoulderFlapLeft,
            ChestBloodFront,
            ChestBloodBack,
            Breath,
            PlayerName,
            Base,
            Head,
            SpellLeftHand,
            SpellRightHand,
            Special1,
            Special2,
            Special3,
            SheathMainHand,
            SheathOffHand,
            SheathShield,
            PlayerNameMounted,
            LargeWeaponLeft,
            LargeWeaponRight,
            HipWeaponLeft,
            HipWeaponRight,
            Chest,
            HandArrow,
            Bullet,
            SpellHandOmni,
            SpellHandDir,
            VehicleSeat1,
            VehicleSeat2,
            VehicleSeat3,
            VehicleSeat4,
            VehicleSeat5,
            VehicleSeat6,
            VehicleSeat7,
            VehicleSeat8,
            LeftFoot,
            RightFoot,
            ShieldNoGlove,
            SpineLow,
            AlteredShoulderRight,
            AlteredShoulderLeft,
            BeltBuckle,
            SheathCrossbow,
            HeadTop
        }
    }
}
