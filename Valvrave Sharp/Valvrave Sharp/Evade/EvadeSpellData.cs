namespace Valvrave_Sharp.Evade
{
    using LeagueSharp;
    using LeagueSharp.SDK.Core.Extensions;
    using LeagueSharp.SDK.Core.UI.IMenu.Values;

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

        public string CheckSpellName = "";

        public int Delay;

        public EvadeTypes EvadeType;

        public bool FixedRange;

        public float MaxRange;

        public string Name;

        public SpellSlot Slot;

        public int Speed;

        public SpellTargets[] ValidTargets;

        private int dangerLevel;

        #endregion

        #region Public Properties

        public int DangerLevel
        {
            get
            {
                return Program.MainMenu["Evade"]["Spells"][this.Name]["DangerLevel"] != null
                           ? Program.MainMenu["Evade"]["Spells"][this.Name]["DangerLevel"].GetValue<MenuSlider>().Value
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
                return Program.MainMenu["Evade"]["Spells"][this.Name]["Enabled"] == null
                       || Program.MainMenu["Evade"]["Spells"][this.Name]["Enabled"].GetValue<MenuBool>().Value;
            }
        }

        public bool IsReady
        {
            get
            {
                return (this.CheckSpellName == ""
                        || Program.Player.Spellbook.GetSpell(this.Slot).Name == this.CheckSpellName)
                       && this.Slot.IsReady();
            }
        }

        #endregion
    }
}