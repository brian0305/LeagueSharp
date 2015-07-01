using BrianSharp.Common;
using LeagueSharp;
using LeagueSharp.Common;

namespace BrianSharp.Evade
{
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
        private int _dangerLevel;
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

        public int DangerLevel
        {
            get
            {
                return Helper.GetItem("ESSS_" + Name, "DangerLevel") != null
                    ? Helper.GetValue<Slider>("ESSS_" + Name, "DangerLevel").Value
                    : _dangerLevel;
            }
            set { _dangerLevel = value; }
        }

        public bool Enabled
        {
            get
            {
                return Helper.GetItem("ESSS_" + Name, "Enabled") == null ||
                       Helper.GetValue<bool>("ESSS_" + Name, "Enabled");
            }
        }

        public bool IsReady
        {
            get
            {
                return (CheckSpellName == "" || ObjectManager.Player.Spellbook.GetSpell(Slot).Name == CheckSpellName) &&
                       Slot.IsReady();
            }
        }
    }
}