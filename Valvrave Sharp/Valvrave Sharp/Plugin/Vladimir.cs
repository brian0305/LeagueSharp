namespace Valvrave_Sharp.Plugin
{
    #region

    using System;
    using System.Drawing;

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Enumerations;
    using LeagueSharp.SDK.UI;
    using LeagueSharp.SDK.Utils;

    using Valvrave_Sharp.Core;

    #endregion

    internal class Vladimir : Program
    {
        #region Static Fields

        private static BuffInstance buffE;

        private static bool haveQ, haveW;

        #endregion

        #region Constructors and Destructors

        public Vladimir()
        {
            Q = new Spell(SpellSlot.Q, 600).SetTargetted(0.25f, float.MaxValue);
            W = new Spell(SpellSlot.W, 350);
            E =
                new Spell(SpellSlot.E, 600).SetSkillshot(0, 40, 4000, false, SkillshotType.SkillshotLine)
                    .SetCharged("VladimirE", "VladimirE", 600, 600, 1.5f);
            R = new Spell(SpellSlot.R, 700).SetSkillshot(
                0.005f,
                375,
                float.MaxValue,
                false,
                SkillshotType.SkillshotCircle);
            Q.DamageType = W.DamageType = E.DamageType = R.DamageType = DamageType.Magical;

            var lhMenu = MainMenu.Add(new Menu("LastHit", "Last Hit"));
            {
                lhMenu.Bool("Q", "Use Q");
            }
            var ksMenu = MainMenu.Add(new Menu("KillSteal", "Kill Steal"));
            {
                ksMenu.Bool("Q", "Use Q");
                ksMenu.Bool("E", "Use E");
            }
            var drawMenu = MainMenu.Add(new Menu("Draw", "Draw"));
            {
                drawMenu.Bool("Q", "Q Range", false);
                drawMenu.Bool("E", "E Range", false);
                drawMenu.Bool("R", "R Range", false);
            }

            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnBuffAdd += (sender, args) =>
                {
                    if (!sender.IsMe)
                    {
                        return;
                    }
                    switch (args.Buff.DisplayName)
                    {
                        case "VladimirQFrenzy":
                            haveQ = true;
                            break;
                        case "VladimirSanguinePool":
                            haveW = true;
                            break;
                        case "VladimirE":
                            buffE = args.Buff;
                            break;
                    }
                };
            Obj_AI_Base.OnBuffRemove += (sender, args) =>
                {
                    if (!sender.IsMe)
                    {
                        return;
                    }
                    switch (args.Buff.DisplayName)
                    {
                        case "VladimirQFrenzy":
                            haveQ = false;
                            break;
                        case "VladimirSanguinePool":
                            haveW = false;
                            break;
                        case "VladimirE":
                            buffE = null;
                            break;
                    }
                };
            GameObjectNotifier<MissileClient>.OnCreate += (sender, client) =>
                {
                    if (buffE == null || !client.SpellCaster.IsMe || client.SData.Name != "VladimirEMissile")
                    {
                        return;
                    }
                    buffE = null;
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
            if (MainMenu["Draw"]["Q"] && Q.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
            if (MainMenu["Draw"]["E"] && E.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
            if (MainMenu["Draw"]["R"] && R.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
        }

        #endregion
    }
}