namespace Valvrave_Sharp.Plugin
{
    #region

    using System;
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

    internal class DrMundo : Program
    {
        #region Static Fields

        private static bool haveW;

        #endregion

        #region Constructors and Destructors

        public DrMundo()
        {
            Q = new Spell(SpellSlot.Q, 1050).SetSkillshot(0.25f, 60, 2000, true, SkillshotType.SkillshotLine);
            W = new Spell(SpellSlot.W, 325);
            E = new Spell(SpellSlot.E, 25);
            R = new Spell(SpellSlot.R, 0);
            Q.DamageType = W.DamageType = DamageType.Magical;
            Q.MinHitChance = HitChance.VeryHigh;

            var comboMenu = MainMenu.Add(new Menu("Combo", "Combo"));
            {
                comboMenu.Separator("Q/E: Always On");
                comboMenu.Bool("QCol", "Smite Collision");
                comboMenu.Separator("W Settings");
                comboMenu.Bool("W", "Use W", false);
                comboMenu.Slider("WHpA", "If Hp >= (%)", 10);
                comboMenu.Slider("WRange", "Extra Range", 60, 0, 200);
                comboMenu.Separator("R Settings");
                comboMenu.Bool("R", "Use R");
                comboMenu.Slider("RHpU", "If Hp < (%)", 20);
            }
            var hybridMenu = MainMenu.Add(new Menu("Hybrid", "Hybrid"));
            {
                hybridMenu.Bool("Q", "Use Q");
                hybridMenu.Bool("E", "Use E");
                hybridMenu.Separator("Auto Q Settings");
                hybridMenu.KeyBind("AutoQ", "KeyBind", Keys.T, KeyBindType.Toggle);
                hybridMenu.Slider("AutoQHpA", "If Hp >= (%)", 20);
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
                drawMenu.Bool("W", "W Range", false);
            }

            Game.OnUpdate += OnUpdate;
            Drawing.OnEndScene += OnEndScene;
            Variables.Orbwalker.OnAction += OnAction;
            AttackableUnit.OnDamage += (sender, args) =>
                {
                    if (args.TargetNetworkId != Player.NetworkId
                        || Variables.Orbwalker.ActiveMode != OrbwalkingMode.Combo || !R.IsReady())
                    {
                        return;
                    }
                    if (MainMenu["Combo"]["R"] && Player.HealthPercent < MainMenu["Combo"]["RHpU"])
                    {
                        R.Cast();
                    }
                };
            Obj_AI_Base.OnBuffAdd += (sender, args) =>
                {
                    if (!sender.IsMe || args.Buff.DisplayName != "BurningAgony")
                    {
                        return;
                    }
                    haveW = true;
                };
            Obj_AI_Base.OnBuffRemove += (sender, args) =>
                {
                    if (!sender.IsMe || args.Buff.DisplayName != "BurningAgony")
                    {
                        return;
                    }
                    haveW = false;
                };
        }

        #endregion

        #region Properties

        private static Obj_AI_Hero GetETarget
            =>
                Variables.TargetSelector.GetTargets(Player.GetRealAutoAttackRange() + 200, DamageType.Physical)
                    .FirstOrDefault(
                        i =>
                        i.DistanceToPlayer() < i.GetRealAutoAttackRange() + (Player.AttackRange < 150 ? E.Range : 0));

        #endregion

        #region Methods

        private static void AutoQ()
        {
            if (!Q.IsReady() || !MainMenu["Hybrid"]["AutoQ"].GetValue<MenuKeyBind>().Active
                || Player.HealthPercent < MainMenu["Hybrid"]["AutoQHpA"])
            {
                return;
            }
            Q.CastingBestTarget();
        }

        private static void Combo()
        {
            if (MainMenu["Combo"]["R"] && R.IsReady() && Player.HealthPercent < MainMenu["Combo"]["RHpU"]
                && Player.CountEnemyHeroesInRange(600) > 0 && R.Cast())
            {
                return;
            }
            if (Q.CastingBestTarget().IsCasted())
            {
                return;
            }
            if (Q.IsReady())
            {
                var target = Q.GetTarget(Q.Width / 2);
                if (target != null)
                {
                    Q.CastSpellSmite(target, MainMenu["Combo"]["QCol"]);
                }
            }
            if (E.IsReady() && !Player.Spellbook.IsAutoAttacking && !Variables.Orbwalker.CanAttack
                && Variables.Orbwalker.CanMove)
            {
                var target = GetETarget;
                if (target != null
                    && (target.Health + target.PhysicalShield <= GetEDmg(target) || !target.InAutoAttackRange())
                    && E.Cast())
                {
                    return;
                }
            }
            if (MainMenu["Combo"]["W"] && W.IsReady())
            {
                if (Player.HealthPercent >= MainMenu["Combo"]["WHpA"]
                    && W.GetTarget(MainMenu["Combo"]["WRange"]) != null)
                {
                    if (!haveW)
                    {
                        W.Cast();
                    }
                }
                else if (haveW)
                {
                    W.Cast();
                }
            }
        }

        private static double GetEDmg(Obj_AI_Base target)
        {
            return Player.GetAutoAttackDamage(target) + E.GetDamage(target);
        }

        private static void Hybrid()
        {
            if (!Q.IsReady() || !MainMenu["Hybrid"]["Q"])
            {
                return;
            }
            Q.CastingBestTarget();
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
            if (MainMenu["KillSteal"]["E"] && E.IsReady())
            {
                var target = GetETarget;
                if (target != null && target.Health + target.PhysicalShield <= GetEDmg(target) && E.Cast())
                {
                    Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                }
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

        private static void OnAction(object sender, OrbwalkingActionArgs args)
        {
            if (!E.IsReady() || args.Type != OrbwalkingType.AfterAttack || !(args.Target is Obj_AI_Hero))
            {
                return;
            }
            if (Variables.Orbwalker.ActiveMode == OrbwalkingMode.Combo)
            {
                E.Cast();
            }
            else if (Variables.Orbwalker.ActiveMode == OrbwalkingMode.Hybrid && MainMenu["Hybrid"]["E"])
            {
                E.Cast();
            }
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
        }

        private static void OnUpdate(EventArgs args)
        {
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