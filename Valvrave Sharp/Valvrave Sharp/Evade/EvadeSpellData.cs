namespace Valvrave_Sharp.Evade
{
    #region

    using LeagueSharp;

    #endregion

    internal enum SpellValidTargets
    {
        AllyMinions,

        EnemyMinions,

        AllyWards,

        EnemyWards,

        AllyChampions,

        EnemyChampions,

        AllyObjects
    }

    internal class EvadeSpellData
    {
        #region Fields

        internal bool CanShieldAllies;

        internal string CheckBuffName = "";

        internal string CheckSpellName = "";

        internal int Delay;

        internal bool Invert;

        internal bool IsBlink;

        internal bool IsDash;

        internal bool IsFixedRange;

        internal bool IsInvulnerability;

        internal bool IsShield;

        internal bool IsSpellShield;

        internal bool IsYasuoWall;

        internal string Name;

        internal float Range;

        internal string RequireBuff = "";

        internal bool RequireMissilePos;

        internal bool RequirePreMove;

        internal bool SelfCast;

        internal SpellSlot Slot;

        internal int Speed;

        internal bool UnderTower;

        internal SpellValidTargets[] ValidTargets;

        private int dangerLevel;

        #endregion

        #region Properties

        internal int DangerLevel
        {
            get
            {
                return Program.MainMenu["Evade"]["Spells"][this.Name]["DangerLevel"] ?? this.dangerLevel;
            }
            set
            {
                this.dangerLevel = value;
            }
        }

        internal bool Enable => Program.MainMenu["Evade"]["Spells"][this.Name]["Enabled"];

        internal bool IsReady
            =>
                (this.CheckSpellName == ""
                 || Program.Player.Spellbook.GetSpell(this.Slot).SData.Name.ToLower() == this.CheckSpellName)
                && Program.Player.Spellbook.CanUseSpell(this.Slot) == SpellState.Ready;

        internal bool IsTargetted => this.ValidTargets != null;

        #endregion
    }

    internal class DashData : EvadeSpellData
    {
        #region Constructors and Destructors

        internal DashData(
            string name,
            SpellSlot slot,
            float range,
            bool fixedRange,
            int delay,
            int speed,
            int dangerLevel)
        {
            this.Name = name;
            this.Range = range;
            this.Slot = slot;
            this.IsFixedRange = fixedRange;
            this.Delay = delay;
            this.Speed = speed;
            this.DangerLevel = dangerLevel;
            this.IsDash = true;
        }

        #endregion
    }

    internal class BlinkData : EvadeSpellData
    {
        #region Constructors and Destructors

        internal BlinkData(string name, SpellSlot slot, float range, int delay, int dangerLevel)
        {
            this.Name = name;
            this.Range = range;
            this.Slot = slot;
            this.Delay = delay;
            this.DangerLevel = dangerLevel;
            this.IsBlink = true;
        }

        #endregion
    }

    internal class InvulnerabilityData : EvadeSpellData
    {
        #region Constructors and Destructors

        internal InvulnerabilityData(string name, SpellSlot slot, int delay, int dangerLevel)
        {
            this.Name = name;
            this.Slot = slot;
            this.Delay = delay;
            this.DangerLevel = dangerLevel;
            this.IsInvulnerability = true;
        }

        #endregion
    }

    internal class ShieldData : EvadeSpellData
    {
        #region Constructors and Destructors

        internal ShieldData(string name, SpellSlot slot, int delay, int dangerLevel, bool isSpellShield = false)
        {
            this.Name = name;
            this.Slot = slot;
            this.Delay = delay;
            this.DangerLevel = dangerLevel;
            this.IsSpellShield = isSpellShield;
            this.IsShield = !this.IsSpellShield;
        }

        #endregion
    }
}