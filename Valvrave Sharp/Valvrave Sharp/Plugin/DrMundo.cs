namespace Valvrave_Sharp.Plugin
{
    #region

    using System;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Core.UI.IMenu.Values;
    using LeagueSharp.SDK.Core.Utils;
    using LeagueSharp.SDK.Core.Wrappers.Damages;

    using Valvrave_Sharp.Core;

    using Menu = LeagueSharp.SDK.Core.UI.IMenu.Menu;

    #endregion

    internal class DrMundo : Program
    {
        #region Static Fields

        private static bool haveW;

        #endregion

        #region Constructors and Destructors

        public DrMundo()
        {
            Q = new Spell(SpellSlot.Q, 1050).SetSkillshot(0.275f, 60, 2100, true, SkillshotType.SkillshotLine);
            W = new Spell(SpellSlot.W, 325);
            E = new Spell(SpellSlot.E, 25);
            R = new Spell(SpellSlot.R);
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
            Drawing.OnDraw += OnDraw;
            Variables.Orbwalker.OnAction += OnAction;
            AttackableUnit.OnDamage += (sender, args) =>
                {
                    if (args.TargetNetworkId != Player.NetworkId
                        || Variables.Orbwalker.GetActiveMode() != OrbwalkingMode.Combo || !R.IsReady())
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
                    if (!sender.IsMe || !args.Buff.Caster.IsMe)
                    {
                        return;
                    }
                    if (args.Buff.DisplayName == "BurningAgony")
                    {
                        haveW = true;
                    }
                };
            Obj_AI_Base.OnBuffRemove += (sender, args) =>
                {
                    if (!sender.IsMe || !args.Buff.Caster.IsMe)
                    {
                        return;
                    }
                    if (args.Buff.DisplayName == "BurningAgony")
                    {
                        haveW = false;
                    }
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
            if (Q.IsReady())
            {
                var target = Q.GetTarget(Q.Width / 2);
                if (target != null)
                {
                    var pred = Q.GetPrediction(target, false, -1, CollisionableObjects.YasuoWall);
                    if (pred.Hitchance >= Q.MinHitChance)
                    {
                        var col = pred.GetCollision();
                        if ((col.Count == 0 || (MainMenu["Combo"]["QCol"] && Common.CastSmiteKillCollision(col)))
                            && Q.Cast(pred.CastPosition))
                        {
                            return;
                        }
                    }
                }
            }
            if (E.IsReady() && !Player.Spellbook.IsAutoAttacking && !Variables.Orbwalker.CanAttack()
                && Variables.Orbwalker.CanMove())
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
                    i =>
                    (i.IsMinion() || i.IsPet(false)) && i.IsValidTarget(Q.Range) && Q.CanLastHit(i, Q.GetDamage(i))
                    && (i.IsUnderAllyTurret() || (i.IsUnderEnemyTurret() && !Player.IsUnderEnemyTurret())
                        || i.DistanceToPlayer() > i.GetRealAutoAttackRange() + 50
                        || i.Health > Player.GetAutoAttackDamage(i))).OrderByDescending(i => i.MaxHealth).ToList();
            if (minions.Count == 0)
            {
                return;
            }
            foreach (var minion in minions)
            {
                if (
                    Q.Casting(
                        minion,
                        false,
                        CollisionableObjects.Heroes | CollisionableObjects.Minions | CollisionableObjects.YasuoWall)
                        .IsCasted())
                {
                    break;
                }
            }
        }

        private static void OnAction(object sender, OrbwalkingActionArgs args)
        {
            if (!E.IsReady() || args.Type != OrbwalkingType.AfterAttack || !(args.Target is Obj_AI_Hero))
            {
                return;
            }
            if (Variables.Orbwalker.GetActiveMode() == OrbwalkingMode.Combo)
            {
                E.Cast();
            }
            else if (Variables.Orbwalker.GetActiveMode() == OrbwalkingMode.Hybrid && MainMenu["Hybrid"]["E"])
            {
                E.Cast();
            }
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
            switch (Variables.Orbwalker.GetActiveMode())
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
            if (Variables.Orbwalker.GetActiveMode() != OrbwalkingMode.Combo
                && Variables.Orbwalker.GetActiveMode() != OrbwalkingMode.Hybrid)
            {
                AutoQ();
            }
        }

        #endregion
    }
}