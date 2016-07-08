namespace Valvrave_Sharp.Plugin
{
    #region

    using System;
    using System.Linq;
    using System.Windows.Forms;

    using LeagueSharp;
    using LeagueSharp.Data.Enumerations;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Enumerations;
    using LeagueSharp.SDK.Polygons;
    using LeagueSharp.SDK.UI;
    using LeagueSharp.SDK.Utils;

    using SharpDX;

    using Valvrave_Sharp.Core;

    using Color = System.Drawing.Color;
    using Menu = LeagueSharp.SDK.UI.Menu;

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

            var comboMenu = MainMenu.Add(new Menu("Combo", "Combo"));
            {
                comboMenu.Bool("Ignite", "Use Ignite");
                comboMenu.Bool("Q", "Use Q");
                comboMenu.Separator("R Settings");
                comboMenu.Bool("R", "Use R");
                comboMenu.Slider("RHpU", "If Enemies Hp < (%) And Hit >= 2", 60);
                comboMenu.Slider("RCountA", "Or Count >=", 2, 1, 5);
                comboMenu.Separator("Zhonya Settings For R Combo");
                comboMenu.Bool("Zhonya", "Use Zhonya");
                comboMenu.Slider("ZhonyaHpU", "If Hp < (%)", 20);
            }
            var hybridMenu = MainMenu.Add(new Menu("Hybrid", "Hybrid"));
            {
                hybridMenu.Separator("Q: Always On");
                hybridMenu.Bool("QLastHit", "Last Hit (Stack < 2)");
                hybridMenu.Bool("E", "Use E", false);
            }
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
            MainMenu.KeyBind("FleeW", "Use W To Flee", Keys.C);

            Game.OnUpdate += OnUpdate;
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

        private static float GetEDmg(Obj_AI_Base target)
        {
            float minDmg = E.GetDamage(target), maxDmg = E.GetDamage(target, DamageStage.Empowered);
            return buffE != null ? Math.Min(minDmg + (Game.Time - buffE.StartTime) * (maxDmg - minDmg), maxDmg) : minDmg;
        }

        private static void Hybrid()
        {
            if ((haveQ || buffE == null) && Q.CastOnBestTarget().IsCasted())
            {
                return;
            }
            if (MainMenu["Hybrid"]["QLastHit"] && Q.IsReady() && !haveQ)
            {
                var minion =
                    GameObjects.EnemyMinions.Where(
                        i =>
                        (i.IsMinion() || i.IsPet(false)) && i.IsValidTarget(Q.Range) && Q.CanLastHit(i, Q.GetDamage(i)))
                        .MaxOrDefault(i => i.MaxHealth);
                if (minion != null)
                {
                    Q.CastOnUnit(minion);
                }
            }
            if (MainMenu["Hybrid"]["E"] && E.IsReady())
            {
            }
        }

        private static void KillSteal()
        {
            if (MainMenu["KillSteal"]["Q"] && Q.IsReady())
            {
                var target =
                    Variables.TargetSelector.GetTargets(Q.Range, Q.DamageType)
                        .FirstOrDefault(
                            i =>
                            i.Health + i.MagicalShield
                            <= Q.GetDamage(i, !haveQ ? DamageStage.Default : DamageStage.Empowered));
                if (target != null)
                {
                    Q.CastOnUnit(target);
                }
            }
            if (MainMenu["KillSteal"]["E"] && E.IsReady())
            {
                var target =
                    Variables.TargetSelector.GetTargets(E.Range, E.DamageType)
                        .FirstOrDefault(i => i.Health + i.MagicalShield <= E.GetDamage(i));
                if (target != null)
                {
                    if (E.IsCharging)
                    {
                        E.Cast(Player.Position);
                    }
                    else
                    {
                        E.StartCharging();
                    }
                }
            }
        }

        private static void LastHit()
        {
            if (!MainMenu["LastHit"]["Q"] || !Q.IsReady() || Player.Spellbook.IsAutoAttacking)
            {
                return;
            }
            var minion =
                GameObjects.EnemyMinions.Where(
                    i =>
                    (i.IsMinion() || i.IsPet(false)) && i.IsValidTarget(Q.Range)
                    && Q.CanLastHit(i, Q.GetDamage(i, !haveQ ? DamageStage.Default : DamageStage.Empowered)))
                    .MaxOrDefault(i => i.MaxHealth);
            if (minion == null)
            {
                return;
            }
            Q.CastOnUnit(minion);
        }

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
            for (var i = 0; i < 360; i += 18)
            {
                var pos = Player.Position
                          + E.Range * new Vector2(1, 0).ToVector3().Rotated((float)(Math.PI * i / 180.0));
                var rect = new RectanglePoly(Player.Position, pos, E.Width);
                rect.Draw(Color.Red);
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || MenuGUI.IsShopOpen || Player.IsRecalling())
            {
                return;
            }
            KillSteal();
            Variables.Orbwalker.SetAttackState(!haveW);
            switch (Variables.Orbwalker.GetActiveMode())
            {
                case OrbwalkingMode.Combo:
                    //Combo();
                    break;
                case OrbwalkingMode.Hybrid:
                    Hybrid();
                    break;
                case OrbwalkingMode.LastHit:
                    LastHit();
                    break;
                case OrbwalkingMode.None:
                    if (MainMenu["FleeW"].GetValue<MenuKeyBind>().Active)
                    {
                        Variables.Orbwalker.Move(Game.CursorPos);
                        if (W.IsReady() && !haveW)
                        {
                            W.Cast();
                        }
                    }
                    break;
            }
        }

        #endregion
    }
}