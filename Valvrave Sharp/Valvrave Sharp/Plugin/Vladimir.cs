namespace Valvrave_Sharp.Plugin
{
    #region

    using System;
    using System.Drawing;

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Enumerations;
    using LeagueSharp.SDK.Utils;

    #endregion

    internal class Vladimir : Program
    {
        #region Constructors and Destructors

        public Vladimir()
        {
            Q = new Spell(SpellSlot.Q, 600).SetTargetted(0.25f, float.MaxValue);
            W = new Spell(SpellSlot.W, 350);
            E = new Spell(SpellSlot.E, 600).SetSkillshot(0, 1, 1, false, SkillshotType.SkillshotLine)
                .SetCharged("", "", 600, 600, 1);
            R = new Spell(SpellSlot.R, 700).SetSkillshot(
                0.25f,
                375,
                float.MaxValue,
                false,
                SkillshotType.SkillshotCircle);
            Q.DamageType = W.DamageType = E.DamageType = R.DamageType = DamageType.Magical;

            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
                {
                    if (sender.IsMe)
                    {
                        Game.PrintChat("=> {0}: {1}", args.SData.Name, args.Slot);
                    }
                };
            Obj_AI_Base.OnBuffAdd += (sender, args) =>
                {
                    if (sender.IsMe)
                    {
                        Game.PrintChat(
                            "{0} ({1}): {2}",
                            args.Buff.DisplayName,
                            args.Buff.Name,
                            args.Buff.EndTime - args.Buff.StartTime);
                    }
                };
            Obj_AI_Base.OnDoCast += (sender, args) =>
                {
                    if (sender.IsMe)
                    {
                        Game.PrintChat("~> {0}: {1}", args.SData.Name, args.Slot);
                    }
                };
        }

        #endregion

        #region Methods

        private static void OnDraw(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }
            Render.Circle.DrawCircle(Player.Position, R.Range, Color.Red);
            Render.Circle.DrawCircle(Player.Position, R.Width, Color.Red);
        }

        #endregion
    }
}