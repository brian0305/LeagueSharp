namespace Valvrave_Sharp.Plugin
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;

    using LeagueSharp;
    using LeagueSharp.Data.Enumerations;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Enumerations;
    using LeagueSharp.SDK.UI;
    using LeagueSharp.SDK.Utils;

    using Valvrave_Sharp.Core;

    using Menu = LeagueSharp.SDK.UI.Menu;

    #endregion

    internal class Kennen : Program
    {
        #region Static Fields

        private static readonly Items.Item Wooglet = new Items.Item(3090, 0);

        private static readonly Items.Item Zhonya = new Items.Item(3157, 0);

        private static bool haveE, haveR;

        #endregion

        #region Constructors and Destructors

        public Kennen()
        {
            Q = new Spell(SpellSlot.Q, 1050).SetSkillshot(0.19f, 50, 1700, true, SkillshotType.SkillshotLine);
            W = new Spell(SpellSlot.W, 800).SetTargetted(0.25f, float.MaxValue);
            E = new Spell(SpellSlot.E, 0);
            R = new Spell(SpellSlot.R, 550).SetTargetted(0.25f, float.MaxValue);
            Q.DamageType = W.DamageType = R.DamageType = DamageType.Magical;
            Q.MinHitChance = HitChance.VeryHigh;

            var comboMenu = MainMenu.Add(new Menu("Combo", "Combo"));
            {
                comboMenu.Bool("Ignite", "Use Ignite");
                comboMenu.Bool("Q", "Use Q");
                comboMenu.Bool("W", "Use W");
                comboMenu.Separator("R Settings");
                comboMenu.Bool("R", "Use R");
                comboMenu.Slider("RHpU", "If Enemies Hp < (%) And Hit >= 2", 60);
                comboMenu.Slider("RCountA", "Or Count >=", 3, 1, 5);
                comboMenu.Separator("Zhonya Settings For R Combo");
                comboMenu.Bool("Zhonya", "Use Zhonya");
                comboMenu.Slider("ZhonyaHpU", "If Hp < (%)", 20);
            }
            var hybridMenu = MainMenu.Add(new Menu("Hybrid", "Hybrid"));
            {
                hybridMenu.Bool("Q", "Use Q");
                hybridMenu.Separator("W Settings");
                hybridMenu.Bool("W", "Use W");
                hybridMenu.Slider("WMpA", "If Mp >=", 100, 0, 200);
                hybridMenu.Separator("Auto Q Settings");
                hybridMenu.KeyBind("AutoQ", "KeyBind", Keys.T, KeyBindType.Toggle);
                hybridMenu.Slider("AutoQMpA", "If Mp >=", 100, 0, 200);
            }
            var lhMenu = MainMenu.Add(new Menu("LastHit", "Last Hit"));
            {
                lhMenu.Bool("Q", "Use Q");
            }
            var ksMenu = MainMenu.Add(new Menu("KillSteal", "Kill Steal"));
            {
                ksMenu.Bool("Q", "Use Q");
                ksMenu.Bool("W", "Use W");
            }
            var drawMenu = MainMenu.Add(new Menu("Draw", "Draw"));
            {
                drawMenu.Bool("Q", "Q Range", false);
                drawMenu.Bool("W", "W Range", false);
                drawMenu.Bool("R", "R Range", false);
            }
            MainMenu.KeyBind("FleeE", "Use E To Flee", Keys.C);

            Game.OnUpdate += OnUpdate;
            Drawing.OnEndScene += OnEndScene;
            Obj_AI_Base.OnBuffAdd += (sender, args) =>
                {
                    if (!sender.IsMe)
                    {
                        return;
                    }
                    switch (args.Buff.DisplayName)
                    {
                        case "KennenLightningRush":
                            haveE = true;
                            break;
                        case "KennenShurikenStorm":
                            haveR = true;
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
                        case "KennenLightningRush":
                            haveE = false;
                            break;
                        case "KennenShurikenStorm":
                            haveR = false;
                            break;
                    }
                };
        }

        #endregion

        #region Properties

        private static List<Obj_AI_Hero> GetWTarget
            =>
                Variables.TargetSelector.GetTargets(W.Range, W.DamageType)
                    .Where(i => HaveW(i) && W.CanHitCircle(i))
                    .ToList();

        #endregion

        #region Methods

        private static void AutoQ()
        {
            if (!Q.IsReady() || !MainMenu["Hybrid"]["AutoQ"].GetValue<MenuKeyBind>().Active
                || Player.Mana < MainMenu["Hybrid"]["AutoQMpA"])
            {
                return;
            }
            Q.CastingBestTarget();
        }

        private static void Combo()
        {
            if (MainMenu["Combo"]["R"])
            {
                if (R.IsReady())
                {
                    var target =
                        Variables.TargetSelector.GetTargets(R.Range + 50, R.DamageType)
                            .Where(i => R.CanHitCircle(i))
                            .ToList();
                    if (target.Count > 0
                        && ((target.Count > 1 && target.Any(i => i.Health + i.MagicalShield <= R.GetDamage(i)))
                            || (target.Count > 1
                                && target.Sum(i => i.HealthPercent) / target.Count <= MainMenu["Combo"]["RHpU"])
                            || target.Count >= MainMenu["Combo"]["RCountA"]) && R.Cast())
                    {
                        return;
                    }
                }
                else if (haveR && MainMenu["Combo"]["Zhonya"] && Player.HealthPercent < MainMenu["Combo"]["ZhonyaHpU"]
                         && Player.CountEnemyHeroesInRange(W.Range) > 0)
                {
                    if (Zhonya.IsReady)
                    {
                        Zhonya.Cast();
                    }
                    if (Wooglet.IsReady)
                    {
                        Wooglet.Cast();
                    }
                }
            }
            if (MainMenu["Combo"]["Q"] && Q.CastingBestTarget().IsCasted())
            {
                return;
            }
            if (MainMenu["Combo"]["W"] && W.IsReady())
            {
                var target = GetWTarget;
                if (target.Count > 0)
                {
                    if (haveR)
                    {
                        if ((target.Count(i => HaveW(i, true)) > 1
                             || target.Any(i => i.Health + i.MagicalShield <= W.GetDamage(i, DamageStage.Empowered))
                             || target.Count > 2 || (target.Count(i => HaveW(i, true)) > 0 && target.Count > 1))
                            && W.Cast())
                        {
                            return;
                        }
                    }
                    else if (W.Cast())
                    {
                        return;
                    }
                }
            }
            var subTarget = W.GetTarget();
            if (subTarget != null && MainMenu["Combo"]["Ignite"] && Common.CanIgnite && subTarget.HealthPercent < 30
                && subTarget.DistanceToPlayer() <= IgniteRange)
            {
                Player.Spellbook.CastSpell(Ignite, subTarget);
            }
        }

        private static bool HaveW(Obj_AI_Base target, bool checkCanStun = false)
        {
            var buff = target.GetBuffCount("KennenMarkOfStorm");
            return buff > 0 && (!checkCanStun || buff == 2);
        }

        private static void Hybrid()
        {
            if (MainMenu["Hybrid"]["Q"] && Q.CastingBestTarget().IsCasted())
            {
                return;
            }
            if (MainMenu["Hybrid"]["W"] && W.IsReady() && Player.Mana >= MainMenu["Hybrid"]["WMpA"]
                && GetWTarget.Count > 0)
            {
                W.Cast();
            }
        }

        private static void KillSteal()
        {
            if (MainMenu["KillSteal"]["Q"] && Q.IsReady())
            {
                var target = Q.GetTarget(Q.Width / 2);
                if (target != null && target.Health + target.MagicalShield <= Q.GetDamage(target)
                    && Q.Casting(
                        target,
                        false,
                        CollisionableObjects.Heroes | CollisionableObjects.Minions | CollisionableObjects.YasuoWall)
                           .IsCasted())
                {
                    return;
                }
            }
            if (MainMenu["KillSteal"]["W"] && W.IsReady()
                && GetWTarget.Any(i => i.Health + i.MagicalShield <= W.GetDamage(i, DamageStage.Empowered)))
            {
                W.Cast();
            }
        }

        private static void LastHit()
        {
            if (!MainMenu["LastHit"]["Q"] || !Q.IsReady() || Player.Spellbook.IsAutoAttacking)
            {
                return;
            }
            var minions =
                GameObjects.EnemyMinions.Where(
                    i => (i.IsMinion() || i.IsPet(false)) && i.IsValidTarget(Q.Range) && Q.CanLastHit(i, Q.GetDamage(i)))
                    .OrderByDescending(i => i.MaxHealth)
                    .ToList();
            if (minions.Count == 0)
            {
                return;
            }
            minions.ForEach(
                i =>
                Q.Casting(
                    i,
                    false,
                    CollisionableObjects.Heroes | CollisionableObjects.Minions | CollisionableObjects.YasuoWall));
        }

        private static void OnEndScene(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }
            if (MainMenu["Draw"]["Q"] && Q.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
            if (MainMenu["Draw"]["W"] && W.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
            if (MainMenu["Draw"]["R"] && R.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            Variables.Orbwalker.AttackState = !haveE;
            if (Player.IsDead || MenuGUI.IsChatOpen || MenuGUI.IsShopOpen || Player.IsRecalling())
            {
                return;
            }
            KillSteal();
            switch (Variables.Orbwalker.ActiveMode)
            {
                case OrbwalkingMode.Combo:
                    Combo();
                    break;
                case OrbwalkingMode.Hybrid:
                    Hybrid();
                    break;
                case OrbwalkingMode.LastHit:
                    LastHit();
                    break;
                case OrbwalkingMode.None:
                    if (MainMenu["FleeE"].GetValue<MenuKeyBind>().Active)
                    {
                        Variables.Orbwalker.Move(Game.CursorPos);
                        if (E.IsReady() && !haveE)
                        {
                            E.Cast();
                        }
                    }
                    break;
            }
            if (Variables.Orbwalker.ActiveMode != OrbwalkingMode.Combo
                && Variables.Orbwalker.ActiveMode != OrbwalkingMode.Hybrid)
            {
                AutoQ();
            }
        }

        #endregion
    }
}