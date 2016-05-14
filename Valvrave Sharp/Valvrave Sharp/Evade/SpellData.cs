namespace Valvrave_Sharp.Evade
{
    #region

    using LeagueSharp;
    using LeagueSharp.Data.Enumerations;

    #endregion

    internal class SpellData
    {
        #region Fields

        internal bool AddHitbox;

        internal bool CanBeRemoved;

        internal bool Centered;

        internal CollisionableObjects CollisionObjects;

        internal string ChampionName;

        internal int DangerValue;

        internal int Delay;

        internal bool DisabledByDefault;

        internal bool DisableFowDetection;

        internal bool DontAddExtraDuration;

        internal bool DontCross;

        internal bool DontCheckForDuplicates;

        internal bool DontRemove;

        internal int ExtraDuration;

        internal string[] ExtraMissileNames = { };

        internal int ExtraRange = -1;

        internal string[] ExtraSpellNames = { };

        internal bool FixedRange;

        internal bool FollowCaster;

        internal bool ForceRemove;

        internal string FromObject = "";

        internal string[] FromObjects = { };

        internal bool Invert;

        internal bool IsDangerous;

        internal int MissileAccel = 0;

        internal bool MissileDelayed;

        internal bool MissileFollowsUnit;

        internal int MissileMaxSpeed;

        internal int MissileMinSpeed;

        internal int MissileSpeed;

        internal string MissileSpellName = "";

        internal float MultipleAngle;

        internal int MultipleNumber = -1;

        internal int RingRadius;

        internal SpellSlot Slot;

        internal string SourceObjectName = "";

        internal string SpellName;

        internal bool TakeClosestPath;

        internal string ToggleParticleName = "";

        internal SkillShotType Type;

        #endregion

        #region Properties

        internal int Radius
        {
            get
            {
                return this.RawRadius + Config.SkillShotsExtraRadius
                       + (!this.AddHitbox ? 0 : (int)Program.Player.BoundingRadius);
            }
            set
            {
                this.RawRadius = value;
            }
        }

        internal int Range
        {
            get
            {
                return this.RawRange
                       + (this.Type == SkillShotType.SkillshotLine || this.Type == SkillShotType.SkillshotMissileLine
                              ? Config.SkillShotsExtraRange
                              : 0);
            }
            set
            {
                this.RawRange = value;
            }
        }

        internal int RawRadius { get; private set; }

        internal int RawRange { get; private set; }

        #endregion
    }
}