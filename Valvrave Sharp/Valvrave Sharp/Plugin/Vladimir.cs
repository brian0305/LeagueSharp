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

        private static int lastQ;

        #endregion

        #region Constructors and Destructors

        public Vladimir()
        {
            Q = new Spell(SpellSlot.Q, 600).SetTargetted(0.25f, float.MaxValue);
            W = new Spell(SpellSlot.W, 350);
            E =
                new Spell(SpellSlot.E, 600).SetSkillshot(0, 1, 1, false, SkillshotType.SkillshotLine)
                    .SetCharged("VladimirE", "VladimirE", 600, 600, 1.5f);
            R = new Spell(SpellSlot.R, 700).SetSkillshot(
                0.25f,
                375,
                float.MaxValue,
                false,
                SkillshotType.SkillshotCircle);
            Q.DamageType = W.DamageType = E.DamageType = R.DamageType = DamageType.Magical;

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
                            Game.PrintChat("Q: T");
                            break;
                        case "VladimirSanguinePool":
                            haveW = true;
                            Game.PrintChat("W: T");
                            break;
                        case "VladimirE":
                            buffE = args.Buff;
                            Game.PrintChat("E: T");
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
                            Game.PrintChat("Q: F");
                            break;
                        case "VladimirSanguinePool":
                            haveW = false;
                            Game.PrintChat("W: F");
                            break;
                        case "VladimirE":
                            buffE = null;
                            Game.PrintChat("E: F");
                            break;
                    }
                };
            Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
                {
                    if (sender.IsMe && args.Slot == SpellSlot.Q)
                    {
                        lastQ = Variables.TickCount;
                        Game.PrintChat("Q");
                    }
                };
            Obj_AI_Base.OnDoCast += (sender, args) =>
                {
                    if (sender.IsMe && args.Slot == SpellSlot.Q)
                    {
                        Game.PrintChat("Q: {0}", Variables.TickCount - lastQ);
                    }
                };
            GameObjectNotifier<MissileClient>.OnCreate += (sender, client) =>
                {
                    if (!client.SpellCaster.IsMe)
                    {
                        return;
                    }
                    Game.PrintChat(
                        "{0} - {1} | {2}",
                        client.SData.Name,
                        client.SData.LineWidth,
                        client.SData.MissileSpeed);
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