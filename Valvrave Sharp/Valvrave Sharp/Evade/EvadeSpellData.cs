namespace Valvrave_Sharp.Evade
{
    using LeagueSharp;

    public enum CastTypes
    {
        Position,

        Target,

        Self
    }

    public enum SpellTargets
    {
        AllyMinions,

        EnemyMinions,

        AllyWards,

        EnemyWards,

        AllyChampions,

        EnemyChampions
    }

    public enum EvadeTypes
    {
        Blink,

        Dash,

        Invulnerability,

        MovementSpeedBuff,

        Shield,

        SpellShield,

        WindWall
    }

    internal class EvadeSpellData
    {
        #region Fields

        public CastTypes CastType;

        public string CheckBuffName = "";

        public string CheckSpellName = "";

        public int Delay;

        public EvadeTypes EvadeType;

        public bool ExtraDelay;

        public bool FixedRange;

        public float MaxRange;

        public string Name;

        public SpellSlot Slot;

        public int Speed;

        public bool UnderTower;

        public SpellTargets[] ValidTargets;

        private int dangerLevel;

        #endregion

        #region Public Properties

        public int DangerLevel
        {
            get
            {
                return Program.MainMenu["Evade"] != null
                           ? Program.MainMenu["Evade"]["Spells"][this.Name]["DangerLevel"]
                           : this.dangerLevel;
            }
            set
            {
                this.dangerLevel = value;
            }
        }

        public bool Enabled
        {
            get
            {
                return Program.MainMenu["Evade"]["Spells"][this.Name]["Enabled"];
            }
        }

        public bool IsReady
        {
            get
            {
                return (this.CheckSpellName == ""
                        || ObjectManager.Player.Spellbook.GetSpell(this.Slot).Name == this.CheckSpellName)
                       && ObjectManager.Player.Spellbook.CanUseSpell(this.Slot) == SpellState.Ready;
            }
        }

        #endregion
    }
}