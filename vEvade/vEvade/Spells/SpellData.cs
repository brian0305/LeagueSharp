namespace vEvade.Spells
{
    #region

    using System;

    using LeagueSharp;
    using LeagueSharp.Common;

    using vEvade.Helpers;

    #endregion

    public class SpellData : ICloneable
    {
        #region Fields

        public bool AddHitbox = true;

        public int BehindStart;

        public bool CanBeRemoved = true;

        public CollisionableObjects[] CollisionObjects = { };

        public string ChampName = "";

        public int DangerValue = 1;

        public int Delay = 250;

        public bool DisabledByDefault;

        public bool DontAddExtraDuration;

        public bool DontCross;

        public bool DontCheckForDuplicates;

        public int ExtraDelay;

        public int ExtraDuration;

        public string[] ExtraMissileNames = { };

        public int ExtraRange;

        public string[] ExtraSpellNames = { };

        public bool FixedRange;

        public bool HasEndExplosion;

        public bool HasStartExplosion;

        public int InfrontStart;

        public bool Invert;

        public bool IsDangerous;

        public bool IsDash;

        public bool IsSummoner;

        public string MenuName = "";

        public int MissileAccel;

        public bool MissileDelayed;

        public bool MissileFromUnit;

        public int MissileMaxSpeed;

        public int MissileMinSpeed;

        public string MissileName = "";

        public bool MissileOnly;

        public int MissileSpeed;

        public bool MissileToUnit;

        public float MultipleAngle;

        public int MultipleNumber = -1;

        public bool Perpendicular;

        public int RadiusEx;

        public SpellSlot Slot = SpellSlot.Q;

        public string SpellName = "";

        public bool TakeClosestPath;

        public string ToggleName = "";

        public SpellType Type = SpellType.MissileLine;

        public string TrapName = "";

        #endregion

        #region Public Properties

        public int Radius
        {
            get
            {
                return this.RawRadius
                       + (this.AddHitbox && this.Type != SpellType.Ring && this.Type != SpellType.Cone
                          && this.Type != SpellType.MissileCone && this.Type != SpellType.Arc
                              ? (int)ObjectManager.Player.BoundingRadius
                              : 0);
            }
            set
            {
                this.RawRadius = value;
            }
        }

        public int Range
        {
            get
            {
                return this.RawRange
                       + (!Configs.Debug && (this.Type == SpellType.Line || this.Type == SpellType.MissileLine)
                              ? Configs.ExtraSpellRange
                              : 0);
            }
            set
            {
                this.RawRange = value;
            }
        }

        public int RawRadius { get; private set; }

        public int RawRange { get; private set; }

        #endregion

        #region Public Methods and Operators

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        #endregion
    }

    public enum SpellType
    {
        Line,

        MissileLine,

        Cone,

        MissileCone,

        Circle,

        Ring,

        Arc,

        None
    }
}