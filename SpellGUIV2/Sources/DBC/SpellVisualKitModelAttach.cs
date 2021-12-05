using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.DBC
{
    /**
     * Not loaded from memory, accessed via SQL only
     */
    class SpellVisualKitModelAttach
    {
        public static string LookupAttachmentIndex(uint index) => Enum.GetName(typeof(AttachmentPoint), index);

        public static uint LookupAttachmentIndex(string point) => (uint)Enum.Parse(typeof(AttachmentPoint), point);

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
