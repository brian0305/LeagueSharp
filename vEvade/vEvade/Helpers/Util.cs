namespace vEvade.Helpers
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

    using SharpDX;

    using Color = System.Drawing.Color;

    #endregion

    public static class Util
    {
        #region Static Fields

        public static readonly Random Random = new Random(Utils.GameTimeTickCount);

        #endregion

        #region Public Properties

        public static bool CommonCheck
            =>
                ObjectManager.Player.IsDead || ObjectManager.Player.IsInvulnerable || !ObjectManager.Player.IsTargetable
                || ShieldCheck || ObjectManager.Player.IsCastingInterruptableSpell(true)
                || ObjectManager.Player.IsDashing() || ImmobileCheck;

        public static bool ShieldCheck
        {
            get
            {
                if (ObjectManager.Player.HasBuffOfType(BuffType.SpellShield)
                    || ObjectManager.Player.HasBuffOfType(BuffType.SpellImmunity))
                {
                    return true;
                }

                switch (ObjectManager.Player.ChampionName)
                {
                    case "Olaf":
                        if (ObjectManager.Player.HasBuff("OlafRagnarok"))
                        {
                            return true;
                        }
                        break;
                    case "Sion":
                        if (ObjectManager.Player.HasBuff("SionR"))
                        {
                            return true;
                        }
                        break;
                }

                if (ObjectManager.Player.LastCastedSpellName() == "SivirE"
                    && Utils.TickCount - ObjectManager.Player.LastCastedSpellT() < 300)
                {
                    return true;
                }

                if (ObjectManager.Player.LastCastedSpellName() == "BlackShield"
                    && Utils.TickCount - ObjectManager.Player.LastCastedSpellT() < 300)
                {
                    return true;
                }

                if (ObjectManager.Player.LastCastedSpellName() == "NocturneShit"
                    && Utils.TickCount - ObjectManager.Player.LastCastedSpellT() < 300)
                {
                    return true;
                }

                return false;
            }
        }

        #endregion

        #region Properties

        private static bool ImmobileCheck
        {
            get
            {
                var result = (from buff in ObjectManager.Player.Buffs
                              where
                                  buff.IsValid
                                  && (buff.Type == BuffType.Charm || buff.Type == BuffType.Knockup
                                      || buff.Type == BuffType.Stun || buff.Type == BuffType.Suppression
                                      || buff.Type == BuffType.Snare)
                              select buff.EndTime).Concat(new[] { 0f }).Max();

                return (result.Equals(0) ? -1 : (int)(Utils.GameTimeTickCount + (result - Game.Time) * 1000))
                       - Utils.GameTimeTickCount > Game.Ping / 2 + 70;
            }
        }

        #endregion

        #region Public Methods and Operators

        public static void DrawLine(Vector3 start, Vector3 end, Color color)
        {
            Drawing.DrawLine(Drawing.WorldToScreen(start), Drawing.WorldToScreen(end), 2, color);
        }

        public static void Move(this Vector2 pos)
        {
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, pos.To3D(), false);
        }

        #endregion
    }

    public class SpellList<TK, TV> : Dictionary<TK, TV>
    {
        #region Public Events

        public event EventHandler OnAdd;

        #endregion

        #region Public Methods and Operators

        public new void Add(TK key, TV value)
        {
            this.OnAdd?.Invoke(this, null);
            base.Add(key, value);
        }

        #endregion
    }
}