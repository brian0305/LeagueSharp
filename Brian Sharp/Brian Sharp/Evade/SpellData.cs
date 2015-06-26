using LeagueSharp;

namespace BrianSharp.Evade
{
    public class SpellData
    {
        public bool AddHitbox = false;
        public bool CanBeRemoved = false;
        public bool Centered = false;
        public string ChampionName;
        public CollisionObjectTypes[] CollisionObjects = { };
        public int DangerValue;
        public int Delay;
        public bool DisabledByDefault = false;
        public bool DisableFowDetection = false;
        public bool DontAddExtraDuration = false;
        public bool DontCheckForDuplicates = false;
        public bool DontCross = false;
        public bool DontRemove = false;
        public int ExtraDuration;
        public string[] ExtraMissileNames = { };
        public int ExtraRange = -1;
        public string[] ExtraSpellNames = { };
        public bool FixedRange = false;
        public bool ForceRemove = false;
        public string FromObject = "";
        public string[] FromObjects = { };
        public int Id = -1;
        public bool Invert = false;
        public bool IsDangerous = false;
        public int MissileAccel = 0;
        public bool MissileDelayed = false;
        public bool MissileFollowsUnit = false;
        public int MissileMaxSpeed;
        public int MissileMinSpeed;
        public int MissileSpeed;
        public string MissileSpellName = "";
        public float MultipleAngle;
        public int MultipleNumber = -1;
        public int RingRadius;
        public SpellSlot Slot;
        public string SpellName;
        public string ToggleParticleName = "";
        public SkillShotType Type;

        public string MenuItemName
        {
            get { return ChampionName + " - " + SpellName; }
        }

        public int Radius
        {
            get
            {
                return Configs.SkillShotsExtraRadius + RawRadius +
                       (!AddHitbox ? 0 : (int) ObjectManager.Player.BoundingRadius);
            }
            set { RawRadius = value; }
        }

        public int RawRadius { get; private set; }

        public int Range
        {
            get
            {
                return RawRange +
                       (Type == SkillShotType.SkillshotLine || Type == SkillShotType.SkillshotMissileLine
                           ? Configs.SkillShotsExtraRange
                           : 0);
            }
            set { RawRange = value; }
        }

        public int RawRange { get; private set; }
    }
}